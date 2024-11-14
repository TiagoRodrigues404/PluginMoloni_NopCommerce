using Nop.Plugin.Misc.Moloni.Services.IsolatedStorage;
using MoloniAPI.Services.Utils;
using System.Diagnostics;
using System.Text.Json;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniTokenService
{
    /// <summary>
    /// Serviço responsável por gerenciar tokens de autenticação para a API Moloni.
    /// Inclui métodos para obter, renovar e armazenar tokens de forma segura.
    /// </summary>
    public class MoloniTokenService : IMoloniTokenService
    {
        private readonly IIsolatedStorage _isolatedStorage;
        private readonly IMoloniSettingsProvider _moloniSettings;
        private readonly IUtils _iUtils;
        private readonly HttpClient _httpClient;


        /// <summary>
        /// Construtor da classe MoloniTokenService.
        /// Inicializa as dependências necessárias para interagir com a API Moloni e gerenciar tokens.
        /// </summary>
        /// <param name="isolatedStorage">Serviço de armazenamento isolado para salvar e carregar tokens.</param>
        /// <param name="utils">Serviço de utilidades, incluindo verificações de validade de tokens e políticas de retry.</param>
        /// <param name="moloniSettings">Configurações da API Moloni, como ClientId e ClientSecret.</param>
        /// <param name="httpClientFactory">Fábrica de HttpClient para criar instâncias de HttpClient.</param>
        public MoloniTokenService(IIsolatedStorage isolatedStorage,
                                  IUtils utils,
                                  IHttpClientFactory httpClientFactory,
                                  IMoloniSettingsProvider moloniSettingsProvider
            )
        {
            _isolatedStorage = isolatedStorage;
            _iUtils = utils;
            _httpClient = httpClientFactory.CreateClient("MoloniAPI");
            _moloniSettings = moloniSettingsProvider;
        }

        /// <summary>
        /// Obtém um token de acesso.
        /// </summary>
        /// <returns>Retorna o token de acesso válido ou null se não for possível obter um token.</returns>
        public async Task<string?> GetAccessTokenAsync()
        {
            var tokenResponse = await GetToken();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                Debug.WriteLine("Não foi possível obter o token de acesso.");
                return null;
            }
            return tokenResponse.AccessToken;
        }

        /// <summary>
        /// Obtém o token de autenticação da API Moloni.
        /// Verifica se o token atual é válido, se está próximo da expiração ou se precisa ser renovado.
        /// </summary>
        /// <returns>Retorna um objeto MoloniTokenResponse contendo o token de autenticação.</returns>
        private async Task<MoloniTokenResponse?> GetToken()
        {
            var currentToken = _isolatedStorage.LoadTokensFromIsolatedStorage();

            if (currentToken != null)
            {
                if (_iUtils.HasMoreThan14Days(currentToken))
                {
                    return await GetMoloniTokenAsync();
                }
                else if (_iUtils.IsTokenNearExpiry(currentToken))
                {
                    return await GetMoloniRefreshTokenAsync(currentToken);
                }
                return currentToken;
            }
            else
            {
                return await GetMoloniTokenAsync();
            }
        }

        /// <summary>
        /// Obtém um novo token de autenticação da API Moloni usando as credenciais fornecidas.
        /// </summary>
        /// <returns>Retorna um objeto MoloniTokenResponse contendo o novo token de autenticação.</returns>
        private async Task<MoloniTokenResponse?> GetMoloniTokenAsync()
        {
            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var queryParams = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", settings.ClientId },
                { "client_secret", settings.ClientSecret },
                { "username", settings.Username },
                { "password", settings.Password }
            };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            HttpResponseMessage response = await _iUtils.ExecuteAsync(() =>
            {
                return _httpClient.GetAsync($"grant/?{queryString}");
            });

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    var moloniTokenResponse = System.Text.Json.JsonSerializer.Deserialize<MoloniTokenResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (moloniTokenResponse != null)
                    {
                        var tokenToSave = new MoloniTokenResponse
                        {
                            AccessToken = moloniTokenResponse.AccessToken,
                            RefreshToken = moloniTokenResponse.RefreshToken,
                            ExpiresIn = moloniTokenResponse.ExpiresIn,
                            TokenType = moloniTokenResponse.TokenType,
                            Scope = moloniTokenResponse.Scope,
                            CreatedAt = moloniTokenResponse.CreatedAt
                        };

                        _isolatedStorage.SaveTokensToIsolatedStorage(tokenToSave);
                    }

                    return moloniTokenResponse;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro inesperado: {ex.Message}");
                    return null;
                }
            }
            else
            {
                Debug.WriteLine("Erro ao buscar token: " + response.StatusCode);
                return null;
            }
        }

        /// <summary>
        /// Renova o token de autenticação da API Moloni usando o token de refresh.
        /// </summary>
        /// <param name="currentToken">O token de autenticação atual que será renovado.</param>
        /// <returns>Retorna um objeto MoloniTokenResponse contendo o novo token de autenticação.</returns>
        private async Task<MoloniTokenResponse?> GetMoloniRefreshTokenAsync(MoloniTokenResponse currentToken)
        {
            MoloniSettings settings = await _moloniSettings.GetMoloniSettingsAsync();

            var queryParams = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "client_id", settings.ClientId },
                { "client_secret", settings.ClientSecret },
                { "refresh_token", currentToken.RefreshToken }
            };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));

            HttpResponseMessage response = await _iUtils.ExecuteAsync(() =>
            {
                return _httpClient.GetAsync($"grant/?{queryString}");
            });

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                try
                {
                    var moloniTokenResponse = System.Text.Json.JsonSerializer.Deserialize<MoloniTokenResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (moloniTokenResponse != null)
                    {
                        var tokenToSave = new MoloniTokenResponse
                        {
                            AccessToken = moloniTokenResponse.AccessToken,
                            RefreshToken = moloniTokenResponse.RefreshToken,
                            ExpiresIn = moloniTokenResponse.ExpiresIn,
                            TokenType = moloniTokenResponse.TokenType,
                            Scope = moloniTokenResponse.Scope,
                            CreatedAt = moloniTokenResponse.CreatedAt
                        };

                        _isolatedStorage.SaveTokensToIsolatedStorage(tokenToSave);
                    }

                    return moloniTokenResponse;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro inesperado: {ex.Message}");
                    return null;
                }
            }
            else
            {
                Debug.WriteLine("Erro ao buscar token: " + response.StatusCode);
                return null;
            }
        }
    }
}
