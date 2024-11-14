using Nop.Plugin.Misc.Moloni.Models;

namespace MoloniAPI.Services.Utils
{
    /// <summary>
    /// Interface para a classe utilitária que fornece métodos auxiliares para manipulação de tokens e execução de operações HTTP com políticas de retry.
    /// </summary>
    public interface IUtils
    {
        /// <summary>
        /// Verifica se o token de autenticação está próximo de expirar, com uma margem de segurança.
        /// Esse método é útil para saber se um novo token deve ser solicitado antes do tempo de expiração.
        /// </summary>
        /// <param name="tokenResponse">O token de autenticação que será verificado.</param>
        /// <returns>Retorna true se o token estiver próximo de expirar, ou false caso contrário.</returns>
        bool IsTokenNearExpiry(MoloniTokenResponse tokenResponse);

        /// <summary>
        /// Verifica se o token de autenticação foi criado há mais de 14 dias, o que pode indicar a necessidade de renovação.
        /// Esse método é usado para garantir que o token seja atualizado regularmente, evitando problemas de segurança ou expiração.
        /// </summary>
        /// <param name="tokenResponse">O token de autenticação que será verificado.</param>
        /// <returns>Retorna true se o token tiver mais de 14 dias, ou false caso contrário.</returns>
        bool HasMoreThan14Days(MoloniTokenResponse tokenResponse);

        /// <summary>
        /// Executa uma ação HTTP fornecida e captura exceções de requisição, exibindo mensagens de erro para depuração.
        /// Esse método facilita o tratamento de erros em requisições HTTP, permitindo capturar e registrar exceções de forma centralizada.
        /// </summary>
        /// <param name="action">A função assíncrona que representa a solicitação HTTP.</param>
        /// <returns>Resposta HTTP se a solicitação for bem-sucedida; caso contrário, lança uma exceção.</returns>
        Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> action);

        /// <summary>
        /// Envia uma solicitação HTTP POST para o URL especificado com o conteúdo fornecido.
        /// Este método é útil para realizar requisições HTTP ao Moloni, como criação ou atualização de recursos.
        /// </summary>
        /// <param name="url">O URL para o qual a solicitação será enviada.</param>
        /// <param name="content">O conteúdo HTTP a ser enviado na solicitação.</param>
        /// <returns>Retorna a resposta HTTP se a requisição for bem-sucedida ou null em caso de erro.</returns>
        Task<HttpResponseMessage> SendRequestAsync(string url, HttpContent content);

        /// <summary>
        /// Analisa a resposta HTTP JSON e desserializa-a para um objeto do tipo especificado.
        /// Esse método simplifica a conversão de respostas JSON em objetos C# para uso posterior.
        /// </summary>
        /// <typeparam name="T">O tipo de objeto para o qual a resposta JSON será desserializada.</typeparam>
        /// <param name="response">A resposta HTTP contendo o JSON que será analisado.</param>
        /// <returns>Retorna o objeto desserializado do tipo especificado ou o valor padrão do tipo se ocorrer um erro.</returns>
        Task<T?> ParseJsonResponseAsync<T>(HttpResponseMessage response);

    }
}
