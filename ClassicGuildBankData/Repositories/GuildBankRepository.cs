using ClassicGuildBankData.Data;
using ClassicGuildBankData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using ClassicGuildBankData.Tools;

namespace ClassicGuildBankData.Repositories
{
    public class GuildBankRepository
    {
        #region Data Members

        private readonly ClassicGuildBankDbContext _classicGuildBankDbContext;
        private readonly IConfiguration _config;
        #endregion

        #region Constructor

        public GuildBankRepository(IConfiguration config, ClassicGuildBankDbContext classicGuildBankDbContext)
        {
            _classicGuildBankDbContext = classicGuildBankDbContext;
            _config = config;
        }

        #endregion

        #region Public Methods

        public Guild AddGuild(AddGuildModel addGuildModel, ClassicGuildBankUser classicGuildBankUser)
        {

            var guild = new Guild()
            {
                Id = Guid.NewGuid(),
                UserId = classicGuildBankUser.Id,
                Name = addGuildModel.GuildName,
                InviteUrl = GenerateGuildInviteUrl()
            };

            _classicGuildBankDbContext.Guilds.Add(guild);

            AddGuildMember(guild.Id, classicGuildBankUser, save: false);

            _classicGuildBankDbContext.SaveChanges();

            guild.UserIsOwner = true;
            guild.UserCanUpload = true;

            return guild;
        }

        public ItemRequest AddItemRequest(RequestItemsModel requestItemsModel, ClassicGuildBankUser classicGuildBankUser)
        {
            var request = new ItemRequest()
            {
                Id = Guid.NewGuid(),
                CharacterName = requestItemsModel.CharacterName,
                Gold = requestItemsModel.Gold,
                GuildId = requestItemsModel.GuildId,
                UserId = classicGuildBankUser.Id,
                Status = RequestStatus.Pending,
                DateRequested = DateTime.Now
            };

            _classicGuildBankDbContext.ItemRequests.Add(request);

            foreach (var item in requestItemsModel.RequestItemModels)
            {
                var detail = new ItemRequestDetail()
                {
                    Id = Guid.NewGuid(),
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    ItemRequestId = request.Id
                };

                _classicGuildBankDbContext.ItemRequestDetails.Add(detail);
            }

            _classicGuildBankDbContext.SaveChanges();

            return request;
        }

        public IEnumerable<ItemRequestViewModel> GetItemRequestsForGuild(Guid guildId)
        {
            return _classicGuildBankDbContext.ItemRequests
                .Include(i => i.Details)
                .ThenInclude(i => i.Item)
                .Where(i => i.GuildId == guildId)
                .Select(itemRequest => new ItemRequestViewModel
                {
                    Id = itemRequest.Id,
                    CharacterName = itemRequest.CharacterName,
                    DateRequested = itemRequest.DateRequested,
                    Gold = itemRequest.Gold,
                    Reason = itemRequest.Reason,
                    Status = itemRequest.Status,
                    ItemRequestDetails = itemRequest.Details.Select(itemRequesttDetail => new ItemRequestDetailViewModel()
                    {
                        Item = itemRequesttDetail.Item,
                        Quantity = itemRequesttDetail.Quantity
                    }).ToList()
                });
        }


        public IEnumerable<ItemRequestViewModel> GetMyItemRequests(Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            return _classicGuildBankDbContext.ItemRequests
                .Include(i => i.Details)
                .ThenInclude(i => i.Item)
                .Where(i => i.GuildId == guildId && i.UserId == classicGuildBankUser.Id)
                .Select(itemRequest => new ItemRequestViewModel
                {
                    Id = itemRequest.Id,
                    CharacterName = itemRequest.CharacterName,
                    DateRequested = itemRequest.DateRequested,
                    Gold = itemRequest.Gold,
                    Reason = itemRequest.Reason,
                    Status = itemRequest.Status,
                    ItemRequestDetails = itemRequest.Details.Select(itemRequesttDetail => new ItemRequestDetailViewModel()
                    {
                        Item = itemRequesttDetail.Item,
                        Quantity = itemRequesttDetail.Quantity
                    }).ToList()
                });
        }

        public void ApproveItemRequest(Guid itemRequestId, Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            if (!UserCanProcessItemRequests(guildId, classicGuildBankUser.Id))
                throw new UserCannotProcessItemRequestsException();

            UpdateItemRequestStatus(itemRequestId, RequestStatus.Approved, onUpdateStatusAction: CreateWithdrawalTransaction);
        }

        public void DenyItemRequest(Guid itemRequestId, Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            if (!UserCanProcessItemRequests(guildId, classicGuildBankUser.Id))
                throw new UserCannotProcessItemRequestsException();

            UpdateItemRequestStatus(itemRequestId, RequestStatus.Denied);
        }

        public IEnumerable<ItemRequest> GetItemRequestsForUser(ClassicGuildBankUser user)
        {
            return _classicGuildBankDbContext.ItemRequests
                .Include(i => i.Details)
                .Where(i => i.UserId == user.Id);
        }

        public IEnumerable<Character> GetCharacters(Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = _classicGuildBankDbContext.Guilds
                .Include(guild => guild.GuildMembers)
                .Include(guild => guild.Characters)
                    .ThenInclude(character => character.Bags)
                    .ThenInclude(bag => bag.BagSlots)
                    .ThenInclude(bagSlot => bagSlot.Item)
                .Include(guild => guild.Characters)
                    .ThenInclude(character => character.Bags)
                    .ThenInclude(bag => bag.BagItem)
                .Where(guild => guild.Id == guildId && guild.GuildMembers.Any(gm => gm.UserId == classicGuildBankUser.Id))
                .SingleOrDefault();

            return guild?.Characters ?? Enumerable.Empty<Character>();
        }

        public void UpdateGuildCharacter(Character character, Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            if (!UserCanUpload(guildId, classicGuildBankUser.Id))
                throw new UserCannotUploadException();

            var existingCharacter = _classicGuildBankDbContext.Characters.FirstOrDefault(c => c.GuildId == guildId && c.Name == character.Name);

            if (existingCharacter != null)
                _classicGuildBankDbContext.Characters.Remove(existingCharacter);

            _classicGuildBankDbContext.Characters.Add(character);
            
            _classicGuildBankDbContext.SaveChanges();
        }

        public void AddGuildTransactions(Guid guildId, ClassicGuildBankUser user, List<ItemDepositImportModel> deposits)
        {
            if (!UserCanUpload(guildId, user.Id))
                throw new UserCannotUploadException();

            var depositGroups = deposits.GroupBy(deposit => deposit.CharacterName);

            foreach (var depositGroup in depositGroups)
                CreateDepositTransaction(guildId, depositGroup);

            _classicGuildBankDbContext.SaveChanges();
        }        

        public void RemoveMemberFromGuild(Guid guildId, string userId, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuildForOwner(guildId, classicGuildBankUser);

            if (guild.UserId == userId)
                throw new CannotRemoveOwnerFromGuildException();

            var guildMember = _classicGuildBankDbContext.GuildMembers.FirstOrDefault(gm => gm.GuildId == guildId && gm.UserId == userId);

            if (guildMember == null)
                throw new ApplicationException("Unable to locate guild member");

            _classicGuildBankDbContext.GuildMembers.Remove(guildMember);

            _classicGuildBankDbContext.SaveChanges();
        }

        public void RemoveSelfFromGuild(Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuild(guildId);

            if (guild.UserId == classicGuildBankUser.Id)
                throw new CannotRemoveOwnerFromGuildException();

            var guildMember = _classicGuildBankDbContext.GuildMembers.FirstOrDefault(gm => gm.GuildId == guildId && gm.UserId == classicGuildBankUser.Id);

            if (guildMember == null)
                throw new ApplicationException("Unable to locate guild member");

            _classicGuildBankDbContext.GuildMembers.Remove(guildMember);

            _classicGuildBankDbContext.SaveChanges();
        }

        public void ToggleUploadAccess(Guid guildId, string userId, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuildForOwner(guildId, classicGuildBankUser);

            if (guild.UserId == userId)
                throw new CannotRemoveOwnerFromGuildException();

            var guildMember = _classicGuildBankDbContext.GuildMembers.FirstOrDefault(gm => gm.GuildId == guildId && gm.UserId == userId);

            if (guildMember == null)
                throw new ApplicationException("Unable to locate guild member");

            guildMember.CanUpload = !guildMember.CanUpload;

            _classicGuildBankDbContext.SaveChanges();
        }

        public Item GetItemByName(string itemName, string localization)
        {
            switch (localization)
            {
                case "esMX":
                case "esES":
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.EsName == itemName);
                case "ptPT":
                case "ptBR":
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.PtName == itemName);
                case "deDE":
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.DeName == itemName);
                case "frFR":
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.FrName == itemName);
                case "itIT":
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.ItName == itemName);
                case "ruRU":
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.RuName == itemName);
                case "koKR":
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.KoName == itemName);
                case "zhTW":
                case "zhCN":
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.CnName == itemName);
                default:
                    return _classicGuildBankDbContext.Items.FirstOrDefault(c => c.Name == itemName);
            }
        }

        public IEnumerable<Guild> GetGuilds(ClassicGuildBankUser classicGuildBankUser)
        {
            var guilds = _classicGuildBankDbContext.Guilds
                .Include(guild => guild.GuildMembers)
                .Where(guild => guild.UserId == classicGuildBankUser.Id || guild.GuildMembers.Any(member => member.UserId == classicGuildBankUser.Id))
                .ToList();

            foreach (var guild in guilds)
            {
                guild.UserIsOwner = guild.UserId == classicGuildBankUser.Id;
                guild.IsSelected = classicGuildBankUser.LastSelectedGuildId == guild.Id;
                guild.UserCanUpload = guild.UserIsOwner || guild.GuildMembers.Any(member => member.UserId == classicGuildBankUser.Id && member.CanUpload);
            }

            if (guilds.Any() && !guilds.Any(guild => guild.IsSelected))
                guilds.First().IsSelected = true;

            return guilds;
        }

        public void RenameGuild(RenameGuildModel renameGuildModel, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuildForOwner(renameGuildModel.GuildId, classicGuildBankUser);

            guild.Name = renameGuildModel.GuildName;

            _classicGuildBankDbContext.SaveChanges();
        }

        public void DeleteGuild(Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuildForOwner(guildId, classicGuildBankUser);

            _classicGuildBankDbContext.Guilds.Remove(guild);

            _classicGuildBankDbContext.SaveChanges();
        }

        public void DeleteCharacter(Guid guildId, Guid characterId, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuildForOwner(guildId, classicGuildBankUser);

            var character = _classicGuildBankDbContext.Characters.FirstOrDefault(c => c.GuildId == guildId && c.Id == characterId);

            _classicGuildBankDbContext.Characters.Remove(character);

            _classicGuildBankDbContext.SaveChanges();
        }

        public Guild UpdateGuildInviteLink(Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuildForOwner(guildId, classicGuildBankUser);
            _classicGuildBankDbContext.Attach(guild);

            guild.InviteUrl = GenerateGuildInviteUrl();

            _classicGuildBankDbContext.SaveChanges();

            return guild;
        }

        public Guild UpdateGuildPublicLink(Guid guildId, ClassicGuildBankUser classicGuildBankUser, bool linkEnabled)
        {
            var guild = GetGuildForOwner(guildId, classicGuildBankUser);
            _classicGuildBankDbContext.Attach(guild);

            guild.PublicLinkEnabled = linkEnabled;
            guild.PublicUrl = linkEnabled ? GenerateGuildPublicUrl() : String.Empty;

            _classicGuildBankDbContext.SaveChanges();

            return guild;
        }

        public Guild GetGuildFromToken(string inviteToken)
        {
            var guild = _classicGuildBankDbContext.Guilds.FirstOrDefault(g => g.InviteUrl == inviteToken);

            if (guild == null)
                return null;

            return guild;
        }

        public Guild GetGuildFromReadonlyToken(string publicToken)
        {
            var guild = _classicGuildBankDbContext.Guilds
                .Include(g => g.Characters)
                    .ThenInclude(character => character.Bags)
                    .ThenInclude(bag => bag.BagSlots)
                    .ThenInclude(bagSlot => bagSlot.Item)
                .Include(g => g.Characters)
                    .ThenInclude(character => character.Bags)
                    .ThenInclude(bag => bag.BagItem)
                .FirstOrDefault(g => g.PublicUrl == publicToken);

            if (guild == null)
                return null;

            return guild;
        }

        public GuildMember AddGuildMember(string inviteToken, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuildFromToken(inviteToken);
            if (guild == null)
                return null;

            return AddGuildMember(guild.Id, classicGuildBankUser);
        }

        public GuildMember AddGuildMember(Guid guildId, ClassicGuildBankUser classicGuildBankUser, bool save = true)
        {
            var member = _classicGuildBankDbContext.GuildMembers.FirstOrDefault(m => m.GuildId == guildId && m.UserId == classicGuildBankUser.Id);
            if (member != null)
                return member;

            member = new GuildMember()
            {
                GuildId = guildId,
                UserId = classicGuildBankUser.Id,
                DisplayName = classicGuildBankUser.UserName
            };

            _classicGuildBankDbContext.GuildMembers.Add(member);

            if (save)
                _classicGuildBankDbContext.SaveChanges();

            return member;
        }

        public IEnumerable<GuildMemberViewModel> GetGuildMembers(Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            var members = _classicGuildBankDbContext.GuildMembers
                .Include(m => m.Guild)
                .Where(m => m.GuildId == guildId)
                .Select(m => new GuildMemberViewModel
                {
                    UserId = m.UserId,
                    GuildId = m.GuildId,
                    DisplayName = m.DisplayName,
                    CanUpload = m.Guild.UserId == m.UserId || m.CanUpload
                });

            return members;
        }

        public IEnumerable<GuildMembershipViewModel> GetGuildMembership(string userId)
        {
            var members = _classicGuildBankDbContext.GuildMembers
                .Include(m => m.Guild)
                .Where(m => m.UserId == userId).Select(m => new GuildMembershipViewModel
                {
                    UserId = userId,
                    GuildId = m.GuildId,
                    GuildName = m.Guild.Name,
                    DisplayName = m.DisplayName,
                    IsOwner = m.Guild.UserId == userId
                });

            return members;
        }

        public GuildMembershipViewModel UpdateGuildMembership(GuildMembershipViewModel model)
        {
            var membership = _classicGuildBankDbContext.GuildMembers.FirstOrDefault(m => m.GuildId == model.GuildId && m.UserId == model.UserId);
            if (membership == null)
                throw new InvalidOperationException("Cannot update Guild membership.  Membership not found.");

            _classicGuildBankDbContext.Attach(membership);
            membership.DisplayName = model.DisplayName;

            _classicGuildBankDbContext.SaveChanges();

            return model;
        }

        public IEnumerable<ItemListViewModel> GetItemList(Guid guildId)
        {
            var queryable = _classicGuildBankDbContext.BagSlots
              .Include(b => b.Bag)
              .ThenInclude(g => g.Character)
              .Include(b => b.Item)
              .Where(b => b.ItemId != null && b.Bag.Character.GuildId == guildId)
              .AsEnumerable()
              .GroupBy(b => b.Item)
              .Select(g => new ItemListViewModel() { Item = g.Key, Quantity = g.Sum(b => b.Quantity) });

            //if( pageNumber != null && pageSize != null)
            //    queryable = queryable.Skip(pageNumber.Value * pageSize.Value).Take(pageSize.Value);


            return queryable.ToList();
        }

        public Guild GetGuild(Guid guildId)
        {
            var guild = _classicGuildBankDbContext.Guilds.SingleOrDefault(g => g.Id == guildId);

            if (guild == null)
                throw new ApplicationException("Unable to locate guild.");

            return guild;
        }


        public IEnumerable<TransactionViewModel> GetPageOfTransactions(Guid guildId, int page, int pageSize)
        {
            // filter by name and date? sorting? look at ram search for this.

            return _classicGuildBankDbContext.Transactions
                .Include(t => t.TransactionDetails)
                .ThenInclude(i => i.Item)
                .Where(t => t.GuildId == guildId)
                .OrderByDescending(t => t.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(transaction => new TransactionViewModel
                {
                    CharacterName = transaction.CharacterName,
                    TransactionDate = transaction.TransactionDate,
                    Gold = transaction.Money ?? 0,
                    TransactionType = transaction.Type,
                    TransactionDetails = transaction.TransactionDetails.Select(transactionDetail => new TransactionDetailViewModel()
                    {
                        Item = transactionDetail.Item,
                        Quantity = transactionDetail.Quantity
                    }).ToList()
                });
        }

        public int GetTransactionCount(Guid guildId)
        {
            return _classicGuildBankDbContext.Transactions
                .Where(t => t.GuildId == guildId)
                .Count();
        }

        #endregion

        #region Private Methods

        private void CreateDepositTransaction(Guid guildId, IGrouping<string, ItemDepositImportModel> depositGroup)
        {
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                GuildId = guildId,
                CharacterName = depositGroup.Key,
                TransactionDate = DateTime.Now,
                Type = "Deposit",
                Money = 0
            };

            _classicGuildBankDbContext.Transactions.Add(transaction);

            foreach (var deposit in depositGroup)
            {
                transaction.Money += deposit.Money;

                if (deposit.ItemId <= 0)
                    continue;

                var transactionDetail = transaction.TransactionDetails.FirstOrDefault(t => t.ItemId == deposit.ItemId);

                if (transactionDetail == null)
                {
                    transactionDetail = new TransactionDetail()
                    {
                        Id = Guid.NewGuid(),
                        ItemId = deposit.ItemId,
                        TransactionId = transaction.Id
                    };

                    _classicGuildBankDbContext.TransactionDetails.Add(transactionDetail);
                }

                transactionDetail.Quantity += deposit.ItemCount;
            }
        }

        private void CreateWithdrawalTransaction(ItemRequest itemRequest)
        {
            var transaction = new Transaction()
            {
                Id = Guid.NewGuid(),
                GuildId = itemRequest.GuildId,
                CharacterName = itemRequest.CharacterName,
                TransactionDate = DateTime.Now,
                Type = "Withdrawal",
                Money = itemRequest.Gold
            };

            _classicGuildBankDbContext.Transactions.Add(transaction);

            foreach (var itemRequestDetail in itemRequest.Details)
            {
                var transactionDetail = new TransactionDetail()
                {
                    Id = Guid.NewGuid(),
                    ItemId = itemRequestDetail.ItemId,
                    TransactionId = transaction.Id,
                    Quantity = itemRequestDetail.Quantity
                };

                _classicGuildBankDbContext.TransactionDetails.Add(transactionDetail);
            }
        }

        private Guild GetGuildForOwner(Guid guildId, ClassicGuildBankUser classicGuildBankUser)
        {
            var guild = GetGuild(guildId);

            if (guild.UserId != classicGuildBankUser.Id)
                throw new ApplicationException("User is not the owner of this guild.");

            return guild;
        }

        private string GenerateGuildInviteUrl()
        {
            string uid = string.Empty;
            do
            {
                uid = GetUrlToken();
            }
            while (_classicGuildBankDbContext.Guilds.Any(g => g.InviteUrl == uid));

            return uid;
        }

        private string GenerateGuildPublicUrl()
        {
            string uid = string.Empty;
            do
            {
                uid = GetUrlToken();
            }
            while (_classicGuildBankDbContext.Guilds.Any(g => g.PublicUrl == uid));

            return uid;
        }

        private string GetUrlToken()
        {
            return Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");
        }

        private void UpdateItemRequestStatus(Guid itemRequestId, string status, string reason = "", Action<ItemRequest> onUpdateStatusAction = null )
        {
            var itemRequest = _classicGuildBankDbContext.ItemRequests
                .Include(i => i.Details)
                .ThenInclude(i => i.Item)
                .FirstOrDefault(i => i.Id == itemRequestId);

            if (itemRequest.Status == RequestStatus.Approved)
                throw new UnableToUpdateItemRequestStatusException();

            _classicGuildBankDbContext.Attach(itemRequest);

            itemRequest.Status = status;
            itemRequest.Reason = reason;

            if (onUpdateStatusAction != null)
                onUpdateStatusAction(itemRequest);

            _classicGuildBankDbContext.SaveChanges();
        }

        private bool UserCanUpload(Guid guildId, string userId)
        {
            var guild = _classicGuildBankDbContext.Guilds.Include(g => g.GuildMembers).Where(g => g.Id == guildId).FirstOrDefault();
            if (guild == null)
                return false;

            if (guild.UserId == userId)
                return true;

            return guild.GuildMembers.Any(m => m.UserId == userId && m.CanUpload);
        }

        private bool UserCanProcessItemRequests(Guid guildId, string userId)
        {
            var guild = _classicGuildBankDbContext.Guilds.Include(g => g.GuildMembers).Where(g => g.Id == guildId).FirstOrDefault();
            if (guild == null)
                return false;

            if (guild.UserId == userId)
                return true;

            //maybe make this a separate setting in the future?
            return guild.GuildMembers.Any(m => m.UserId == userId && m.CanUpload);
        }

        #endregion

        #region Exceptions

        public class CannotRemoveOwnerFromGuildException : ApplicationException
        {
        }

        public class UserCannotUploadException : ApplicationException
        {
        }

        public class UserCannotProcessItemRequestsException : ApplicationException
        {
        }

        public class UnableToUpdateItemRequestStatusException : ApplicationException
        {
        }

        #endregion
    }
}
