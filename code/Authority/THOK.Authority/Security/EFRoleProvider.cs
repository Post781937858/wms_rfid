﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace THOK.Authority.Security
{
    public class EFRoleProvider : RoleProvider
    {
        public override string ApplicationName
        {
            get;
            set;
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            using (var database = new DataEntities())
            {
                var roles = database.Role.Where(i => roleNames.Contains(i.Name));
                var users = database.User.Where(i => usernames.Contains(i.Username));

                foreach (var role in roles)
                {
                    foreach (var user in users)
                    {
                        role.Users.Add(user);
                    }
                }

                database.SaveChanges();
            }
        }

        public override void CreateRole(string roleName)
        {
            using (var database = new DataEntities())
            {
                database.AddToRole(new Role
                {
                    Name = roleName,
                });
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            using (var database = new DataEntities())
            {
                var role = database.Role.FirstOrDefault(i => i.Name == roleName);
                if (role != null)
                {
                    if (throwOnPopulatedRole)
                    {
                        if (role.Users.Count > 0)
                        {
                            throw new InvalidOperationException("Can't delete role with members.");
                        }
                    }

                    database.DeleteObject(role);
                    database.SaveChanges();
                    return true;
                }

                return false;
            }
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            var result = new HashSet<string>();
            using (var database = new DataEntities())
            {
                var matchingRoles = database.Role.Where(i => i.Name == roleName && i.Users.Where(user => user.Username.Contains(usernameToMatch)).Count() > 0);
                var usernames = matchingRoles.Select(i => i.Users.Select(u => u.Username));
                foreach (var roleUsernames in usernames)
                {
                    foreach (var username in roleUsernames)
                    {
                        result.Add(username);
                    }
                }
            }
            return result.ToArray();
        }

        public override string[] GetAllRoles()
        {
            using (var database = new DataEntities())
            {
                return database.Role.Select(i => i.Name).ToArray();
            }
        }

        public override string[] GetRolesForUser(string username)
        {
            using (var database = new DataEntities())
            {
                var user = database.User.FirstOrDefault(i => i.Username == username);
                return (user != null) ? user.Roles.Select(i => i.Name).ToArray() : null;
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            using (var database = new DataEntities())
            {
                var role = database.Role.FirstOrDefault(i => i.Name == roleName);
                return (role != null) ? role.Users.Select(i => i.Username).ToArray() : null;
            }
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            using (var database = new DataEntities())
            {
                var role = database.Role.FirstOrDefault(i => i.Name == roleName);
                return (role != null) ? role.Users.Where(i => i.Username == username).Count() > 0 : false;
            }
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            using (var database = new DataEntities())
            {
                var roles = database.Role.Where(i => roleNames.Contains(i.Name));
                var users = database.User.Where(i => usernames.Contains(i.Username));
                foreach (var role in roles)
                {
                    foreach (var user in users)
                    {
                        role.Users.Remove(user);
                    }
                }
                database.SaveChanges();
            }
        }

        public override bool RoleExists(string roleName)
        {
            using (var database = new DataEntities())
            {
                return database.Role.FirstOrDefault(i => i.Name == roleName) != null;
            }
        }
    }
}