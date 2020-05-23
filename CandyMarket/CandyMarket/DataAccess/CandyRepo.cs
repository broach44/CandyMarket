﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using CandyMarket.Models;
using Dapper;
using System.Data.SqlClient;


namespace CandyMarket.DataAccess
{
    public class CandyRepo
    {
        string connectionString;

        public CandyRepo(IConfiguration config)
        {
            connectionString = config.GetConnectionString("CandyMarket");
        }

        public IEnumerable<Candy> GetByUserId(int userId)
        {
            var sql = @"
                        select candies.CandyId, candies.CandyName, Candies.Manufacturer, candies.FlavorCategory
                        from UserCandies
	                        join candies
		                        on candies.candyid = usercandies.candyid
                        where usercandies.userid = @userId
                        and userCandies.isConsumed = 0;
                      ";


            using (var db = new SqlConnection(connectionString))
            {
                var parameters = new { UserId = userId };

                var candies = db.Query<Candy>(sql, parameters);

                return candies;
            }
        }

        public User GetUserById(int userId)
        {
            var sql = @"
                        select *
                        from users
                        where userid = @UserId;
                      ";

            using (var db = new SqlConnection(connectionString))
            {
                var parameters = new { UserId = userId };
                var user = db.QueryFirstOrDefault<User>(sql, parameters);
                return user;
            }
        }

        public Candy GetByCandyName(string candyName)
        {
            var sql = @"
                        select *
                        from candies
                        where candyname = @CandyName;
                      ";

            using (var db = new SqlConnection(connectionString))
            {
                var parameters = new { CandyName = candyName };
                var existingCandy = db.QueryFirstOrDefault<Candy>(sql, parameters);
                return existingCandy;
            }
        }

        public Candy GetCandyById(int candyId)
        {
            var sql = @"
                        select *
                        from candies
                        where candyId = @CandyId;
                      ";

            using (var db = new SqlConnection(connectionString))
            {
                var parameters = new { CandyId = candyId };
                var existingCandy = db.QueryFirstOrDefault<Candy>(sql, parameters);
                return existingCandy;
            }
        }

        //Add: Add a new candy to the candy table, and add a new entry to user candies table
        public Candy Add(int userId, Candy candyToAdd)
        {
            var sql = @"insert into Candies(CandyName, Manufacturer, FlavorCategory)
                        output inserted.*
                        values(@CandyName, @Manufacturer, @FlavorCategory);";

            using (var db = new SqlConnection(connectionString))
            {
                var result = db.QueryFirstOrDefault<Candy>(sql, candyToAdd);
                return result;
            }
        }

        //Update: Updates the users candy inventory (create new usercandies entry for table)
        public UserCandy Update(int userId, int candyId)
        {
            //DateTime dateTime = new DateTime();
            DateTime dateTime = DateTime.Now;
            var sql = @"insert into UserCandies(UserId, CandyId, DateReceived, IsConsumed)
                        output inserted.*
                        values(@UserId, @CandyId, @DateReceived, 0);";

            using (var db = new SqlConnection(connectionString))
            {
                var parameters = new { UserId = userId, CandyId = candyId, DateReceived = dateTime };
                var result = db.QueryFirstOrDefault<UserCandy>(sql, parameters);
                return result;
            }
        }

        // EatCandy: updates isConsumed on UserCandy to true
        public UserCandyDetailed EatCandy(int userCandyId)
        {
            var sql = @"update UserCandies
                        set isConsumed = 1
                        where userCandyId = @UserCandyId
                        select uc.*, c.CandyName, c.FlavorCategory
                        from UserCandies uc
                            join Candies c
                            on uc.CandyId = c.CandyId
                        where uc.UserCandyId = @UserCandyId";

            using (var db = new SqlConnection(connectionString))
            {
                var parameters = new { UserCandyId = userCandyId };
                var candyConsumed = db.QueryFirstOrDefault<UserCandyDetailed>(sql, parameters);
                return candyConsumed;
            }
        }

        // ConsumeSpecificCandy: finds UserCandyId for oldest piece of a specific candy and calls EatCandy
        public UserCandyDetailed ConsumeSpecificCandy(int candyId, int userId)
        {
            var sql = @"select UserCandyId
                        from UserCandies
                        where isConsumed = 0
                        and candyId = @CandyId
                        and userId = @UserId
                        order by dateReceived";
            using (var db = new SqlConnection(connectionString))
            {
                var parameters = new
                {
                    CandyId = candyId,
                    UserId = userId
                };
                var CandyToConsume = db.QueryFirstOrDefault<int>(sql, parameters);
                var eatenCandy = EatCandy(CandyToConsume);
                return eatenCandy;
            }
        }

    }
}
