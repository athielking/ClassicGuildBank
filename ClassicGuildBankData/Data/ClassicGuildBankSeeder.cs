using ClassicGuildBankData.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicGuildBankData.Data
{
    public class ClassicGuildBankSeeder
    {
        #region Data Members

        private readonly ClassicGuildBankDbContext _dbContext;

        private readonly UserManager<ClassicGuildBankUser> _userManager;

        #endregion

        #region Constructor

        public ClassicGuildBankSeeder
        (
            ClassicGuildBankDbContext classicGuildBankDbContext,
            UserManager<ClassicGuildBankUser> userManager
        )
        {
            _dbContext = classicGuildBankDbContext;
            _userManager = userManager;
        }

        #endregion

        #region Public Methods

        public async Task Seed()
        {
            _dbContext.Database.Migrate();

            await SeedUsers();
            await SeedItems();
            await SeedData();
        }

        #endregion

        #region Private Methods

        private async Task SeedUsers()
        {
            var user = await _userManager.FindByNameAsync("athielking");
            if (user == null)
            {
                user = new ClassicGuildBankUser()
                {
                    UserName = "athielking",
                    Email = "athielking@gmail.com",
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, "P@ssw0rd!");

                if (result != IdentityResult.Success)
                {
                    throw new InvalidOperationException("Failed to create default user");
                }
            }            
        }

        private async Task SeedData()
        {
            if (!_dbContext.Guilds.Any(g => g.Id == new Guid(fg1Id)))
            {
                var user = await _userManager.FindByNameAsync("athielking");
                var fakeGuild = GetFakeGuild1(user.Id);
                _dbContext.Guilds.Add(fakeGuild);
                await _dbContext.SaveChangesAsync();
            }

        }

        private static string fg1Id = "72D8FD17-9D7B-47E3-882E-E81CD8FB91D0";
        public Guild GetFakeGuild1(string userId)
        {
            return new Guild
            {
                Id = new Guid(fg1Id),
                Name = "Loch Modan Yacht Club",
                UserId = userId, //athielking
                Characters = new List<Character>
                {
                    GetFakeCharacter1()
                },
                GuildMembers = new List<GuildMember>
                {
                    new GuildMember {
                        
                        _GuildId = fg1Id,
                        DisplayName = "athielking",
                        UserId = userId,
                    }
                }
            };
        }

        private static string fc1Id = "1A6C7066-F013-4378-AA56-24C53F963288";
        public Character GetFakeCharacter1()
        {
            var bank = new Bag
            {
                _CharacterId = fc1Id,
                _Id = "F5091DD3-A11F-4864-A6DF-2A78594A90A9",
                isBank = true,
                BagSlots = new List<BagSlot>(),
                BagContainerId = -1
            };

            var bb1 = new Bag()
            {
                _CharacterId = fc1Id,
                _Id = "D69A53A8-BA4D-4FEA-94CE-61CE613D00F6",
                isBank = true,
                BagSlots = new List<BagSlot>(),
                BagContainerId = 5,
                ItemId = 17966
            };

            var backpack = new Bag
            {
                _CharacterId = fc1Id,
                _Id = "484D54A0-77DF-4446-AC56-430CC049042F",
                BagContainerId = 0,
                BagSlots = new List<BagSlot>()
            };

            for (int i = 0; i < 28; i++)
                bank.BagSlots.Add(new BagSlot { BagId = bank.Id, SlotId = i });

            for (int i = 0; i < 18; i++)
                bb1.BagSlots.Add(new BagSlot { BagId = bb1.Id, SlotId = i });

            for (int i = 0; i < 16; i++)
                backpack.BagSlots.Add(new BagSlot { BagId = backpack.Id, SlotId = i });


            bank.BagSlots[0].ItemId = 14047;
            bank.BagSlots[1].ItemId = 13445;
            bank.BagSlots[4].ItemId = 7078;

            backpack.BagSlots[0].ItemId = 19019;
            backpack.BagSlots[1].ItemId = 17182;

            bb1.BagSlots[0].ItemId = 13457;

            return new Character
            {
                _Id = fc1Id,
                Name = "athielking",
                _GuildId = fg1Id,
                Bags = new List<Bag>
                {
                    backpack,
                    bank,
                    bb1
                }
            };
        }

        private async Task SeedItems()
        {
            var itemCount = await _dbContext.Items.CountAsync();
            if (itemCount > 0)
                return;


            var sqlText = File.ReadAllText($"{AppContext.BaseDirectory}/SQL/seed_items.sql");
            var connection = (SqlConnection)_dbContext.Database.GetDbConnection();
            connection.Open();

            using (var cmd = new SqlCommand(sqlText, connection))
            {
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }
            connection.Close();
        }
        #endregion
    }
}
