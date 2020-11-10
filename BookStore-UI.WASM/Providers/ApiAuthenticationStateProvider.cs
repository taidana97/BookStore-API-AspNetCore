using Blazored.LocalStorage;
using BookStore_UI.WASM.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BookStore_UI.WASM.Providers
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly JwtSecurityTokenHandler _tokenHandler;

        public ApiAuthenticationStateProvider(
            ILocalStorageService localStorage,
            JwtSecurityTokenHandler tokenHandler
            )
        {
            _localStorage = localStorage;
            _tokenHandler = tokenHandler;
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var saveToken = await _localStorage.GetItemAsync<string>("authToken");

                if (string.IsNullOrWhiteSpace(saveToken))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var tokenContent = _tokenHandler.ReadJwtToken(saveToken);
                var expiry = tokenContent.ValidTo;

                if (expiry < DateTime.Now)
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Get Claims from token
                var claims = ParseClaims(tokenContent);

                // Build auth user object
                var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));

                // return authenticated person
                return new AuthenticationState(user);

            }
            catch (Exception)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task LoggedIn()
        {
            var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            var tokenContent = _tokenHandler.ReadJwtToken(savedToken);
            var claims = ParseClaims(tokenContent);
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            var authState = Task.FromResult(new AuthenticationState(user));
            NotifyAuthenticationStateChanged(authState);

        }

        public void LoggedOut()
        {
            var nobody = new ClaimsPrincipal(new ClaimsIdentity());
            var authState  = Task.FromResult(new AuthenticationState(nobody));
            NotifyAuthenticationStateChanged(authState);
        }

        private IList<Claim> ParseClaims(JwtSecurityToken tokenContent)
        {
            var claims = tokenContent.Claims.ToList();
            claims.Add(new Claim(ClaimTypes.Name, tokenContent.Subject));

            return claims;
        }
    }
}
