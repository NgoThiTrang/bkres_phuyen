﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using DoAn.Data;
using DoAn.Data.Model;
using DoAn.Service;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using Owin;
using Web.Infrastructure.Core;
using Web.Models;

[assembly: OwinStartup(typeof(DoAn.App_Start.Startup))]

namespace DoAn.App_Start
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Configure the db context, user manager and signin manager to use a single instance per request
            app.CreatePerOwinContext(BkresContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);
            app.CreatePerOwinContext<UserManager<ApplicationUser>>(CreateManager);

            app.UseOAuthAuthorizationServer(new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = new PathString("/oauth/token"),
                Provider = new AuthorizationServerProvider(),
                AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(1440),
                AllowInsecureHttp = true,
            });
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/dang-nhap-he-thong"),
                Provider = new CookieAuthenticationProvider
                {
                    // Enables the application to validate the security stamp when the user logs in.
                    // This is a security feature which is used when you change a password or add an external login to your account.
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(1440),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);
            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            //app.UseFacebookAuthentication(
            //   appId: "1724156397871880",
            //   appSecret: "398039cc7588d52f87a7adcefecc3210");

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "712161982861-4d9bdgfvf6pti1vviifjogopqdqlft56.apps.googleusercontent.com",
            //    ClientSecret = "T0cgiSG6Gi7BKMr-fCCkdErO"
            //});
        }

        public class AuthorizationServerProvider : OAuthAuthorizationServerProvider
        {
            public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
            {
                await Task.Run(() => context.Validated());
            }

            public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
            {
                var allowedOrigin = context.OwinContext.Get<string>("as:clientAllowedOrigin");

                if (allowedOrigin == null) allowedOrigin = "*";

                context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { allowedOrigin });

                UserManager<ApplicationUser> userManager = context.OwinContext.GetUserManager<UserManager<ApplicationUser>>();
                ApplicationUser user;
                try
                {
                    user = await userManager.FindAsync(context.UserName, context.Password);
                }
                catch (Exception ex)
                {
                    // Could not retrieve the user due to error.
                    context.SetError("server_error");
                    context.Rejected();
                    return;
                }
                if (user != null)
                {
                    if (user.LockoutEnabled)
                    {
                        context.Rejected();
                        context.SetError("invalid_grant", "Tài khoản này đã bị khoá.");
                        return;
                    }
                    var groups = ServiceFactory.Get<IApplicationGroupService>().GetListGroupByUserId(user.Id);
                    var groupsViewModels = AutoMapper.Mapper.Map<IEnumerable<ApplicationGroup>, IEnumerable<ApplicationGroupViewModel>>(groups);
                    var roles = userManager.GetRoles(user.Id);
                    ClaimsIdentity identity = await userManager.CreateIdentityAsync(
                                   user,
                                   DefaultAuthenticationTypes.ExternalBearer);
                    string avatar = string.IsNullOrEmpty(user.Avatar) ? "" : user.Avatar;
                    string email = string.IsNullOrEmpty(user.Email) ? "" : user.Email;
                    string fullName = string.IsNullOrEmpty(user.FullName) ? "" : user.FullName;
                    string userAddress = string.IsNullOrEmpty(user.Address) ? "" : user.Address;
                    identity.AddClaim(new Claim("fullName", fullName));
                    identity.AddClaim(new Claim("avatar", avatar));
                    identity.AddClaim(new Claim("email", email));
                    identity.AddClaim(new Claim("username", user.UserName));
                    identity.AddClaim(new Claim("roles", JsonConvert.SerializeObject(roles)));
                    identity.AddClaim(new Claim("groups", JsonConvert.SerializeObject(groupsViewModels)));
                    identity.AddClaim(new Claim(ClaimTypes.StreetAddress, userAddress));
                    //identity.AddClaim(new Claim("permissions", JsonConvert.SerializeObject(permissionViewModels)));
                    var props = new AuthenticationProperties(new Dictionary<string, string>
                    {
                        {"fullName", user.FullName},
                        {"avatar", avatar },
                        {"email", email},
                        {"username", user.UserName},
                        {"roles",JsonConvert.SerializeObject(roles) },
                        {"groups",JsonConvert.SerializeObject(groupsViewModels) },
                        {"streetAddress", userAddress }
                    });
                    context.Validated(new AuthenticationTicket(identity, props));
                }
                else
                {
                    context.Rejected();
                    context.SetError("invalid_grant", "Tài khoản hoặc mật khẩu không đúng");
                }
            }

            public override Task TokenEndpoint(OAuthTokenEndpointContext context)
            {
                foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
                {
                    context.AdditionalResponseParameters.Add(property.Key, property.Value);
                }
                return Task.FromResult<object>(null);
            }
        }

        private static UserManager<ApplicationUser> CreateManager(IdentityFactoryOptions<UserManager<ApplicationUser>> options, IOwinContext context)
        {
            var userStore = new UserStore<ApplicationUser>(context.Get<BkresContext>());
            var owinManager = new UserManager<ApplicationUser>(userStore);
            return owinManager;
        }
    }
}
