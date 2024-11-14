using Newtonsoft.Json;
using Nop.Plugin.Misc.Moloni.Models;
using Polly;
using System.Diagnostics;

namespace MoloniAPI.Services.Utils
{
    /// <summary>
    /// Classe utilitária que fornece métodos auxiliares para manipulação de tokens e execução de operações HTTP com políticas de retry.
    /// </summary>
    public class Utils : IUtils
    {
        private readonly HttpClient _httpClient;

        public Utils(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("MoloniAPI");
        }

        /// <summary>
        /// Verifica se o token de autenticação está próximo de expirar.
        /// </summary>
        /// <param name="tokenResponse">O token de autenticação que será verificado.</param>
        /// <returns>Retorna true se o token estiver próximo de expirar, ou false caso contrário.</returns>
        public bool IsTokenNearExpiry(MoloniTokenResponse tokenResponse)
        {
            TimeSpan timeElapsed = DateTime.Now - tokenResponse.CreatedAt;
            TimeSpan expiresInTimeSpan = TimeSpan.FromSeconds(tokenResponse.ExpiresIn);
            TimeSpan margin = TimeSpan.FromMinutes(5);

            if (timeElapsed >= expiresInTimeSpan - margin)
                return true;

            return false;
        }

        /// <summary>
        /// Verifica se o token de autenticação foi criado há mais de 14 dias.
        /// </summary>
        /// <param name="tokenResponse">O token de autenticação que será verificado.</param>
        /// <returns>Retorna true se o token tiver mais de 14 dias, ou false caso contrário.</returns>
        public bool HasMoreThan14Days(MoloniTokenResponse tokenResponse)
        {
            TimeSpan timeElapsed = DateTime.Now - tokenResponse.CreatedAt;

            if (timeElapsed.TotalDays > 14)
                return true;

            return false;
        }

        /// <summary>
        /// Executa uma ação HTTP fornecida e captura exceções de requisição.
        /// Este método permite gerenciar exceções ao executar uma solicitação HTTP, exibindo uma mensagem de erro para depuração.
        /// </summary>
        /// <param name="action">A função assíncrona que representa a solicitação HTTP.</param>
        /// <returns>Resposta HTTP se a solicitação for bem-sucedida; caso contrário, lança uma exceção.</returns>
        public async Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> action)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"Erro ao realizar a solicitação: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Envia uma solicitação HTTP POST para o URL especificado com o conteúdo fornecido.
        /// Este método executa os seguintes passos:
        /// 1. Realiza a requisição usando o método POST no cliente HTTP.
        /// 2. Verifica se a resposta indica sucesso; caso contrário, exibe uma mensagem de erro e retorna null.
        /// </summary>
        /// <param name="url">O URL para o qual a solicitação será enviada.</param>
        /// <param name="content">O conteúdo HTTP a ser enviado na solicitação.</param>
        /// <returns>Retorna a resposta HTTP se a requisição for bem-sucedida ou null em caso de erro.</returns>
        public async Task<HttpResponseMessage> SendRequestAsync(string url, HttpContent content)
        {
            HttpResponseMessage response = await ExecuteAsync(() =>
            {
                return _httpClient.PostAsync(url, content);
            });

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine("Erro na requisição: " + response.StatusCode);
                return null;
            }

            return response;
        }


        /// <summary>
        /// Analisa a resposta HTTP JSON para um objeto do tipo especificado.
        /// </summary>
        /// <typeparam name="T">O tipo de objeto para o qual a resposta JSON será desserializada.</typeparam>
        /// <param name="response">A resposta HTTP contendo o JSON que será analisado.</param>
        /// <returns>Retorna o objeto desserializado do tipo especificado ou o valor padrão do tipo se ocorrer um erro.</returns>
        public async Task<T?> ParseJsonResponseAsync<T>(HttpResponseMessage response)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"Resposta JSON: {jsonResponse}");

            try
            {
                return JsonConvert.DeserializeObject<T>(jsonResponse);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado: {ex.Message}");
                return default;
            }
        }
    }
}
