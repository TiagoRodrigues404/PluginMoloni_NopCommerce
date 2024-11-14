using Nop.Plugin.Misc.Moloni.Models;
using Nop.Plugin.Misc.Moloni.Services.Cryptography;
using System.IO.IsolatedStorage;
using System.Text.Json;

namespace Nop.Plugin.Misc.Moloni.Services.IsolatedStorage
{
    /// <summary>
    /// Implementação da interface IIsolatedStorage.
    /// Responsável por salvar e carregar tokens de autenticação no armazenamento isolado,
    /// utilizando criptografia para garantir a segurança dos dados.
    /// </summary>
    public class IsolatedStorage : IIsolatedStorage
    {
        private readonly ICryptography _cryptography;

        /// <summary>
        /// Construtor da classe IsolatedStorage.
        /// Inicializa a dependência de criptografia usada para encriptar e desencriptar os tokens.
        /// </summary>
        /// <param name="cryptography">Serviço de criptografia utilizado para proteger os tokens.</param>
        public IsolatedStorage(ICryptography cryptography)
        {
            _cryptography = cryptography;
        }

        /// <summary>
        /// Salva os tokens de autenticação no armazenamento isolado, encriptando-os antes do salvamento.
        /// </summary>
        /// <param name="tokenResponse">Objeto contendo os tokens de autenticação a serem salvos.</param>
        public void SaveTokensToIsolatedStorage(MoloniTokenResponse tokenResponse)
        {
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("tokens.json", FileMode.Create, isoStore))
                {
                    tokenResponse.AccessToken = _cryptography.Encrypt(tokenResponse.AccessToken);
                    tokenResponse.RefreshToken = _cryptography.Encrypt(tokenResponse.RefreshToken);

                    var json = JsonSerializer.Serialize(tokenResponse);

                    using (StreamWriter writer = new StreamWriter(isoStream))
                    {
                        writer.Write(json);
                    }
                }
            }
        }

        /// <summary>
        /// Carrega os tokens de autenticação do armazenamento isolado, desencriptando-os após o carregamento.
        /// </summary>
        /// <returns>Retorna um objeto contendo os tokens de autenticação desencriptados.</returns>
        public MoloniTokenResponse? LoadTokensFromIsolatedStorage()
        {
            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForAssembly())
            {
                if (isoStore.FileExists("tokens.json"))
                {
                    using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("tokens.json", FileMode.Open, isoStore))
                    {
                        using (StreamReader reader = new StreamReader(isoStream))
                        {
                            var json = reader.ReadToEnd();

                            var tokenResponse = JsonSerializer.Deserialize<MoloniTokenResponse>(json);

                            tokenResponse!.AccessToken = _cryptography.Decrypt(tokenResponse.AccessToken);
                            tokenResponse.RefreshToken = _cryptography.Decrypt(tokenResponse.RefreshToken);

                            return tokenResponse;
                        }
                    }
                }
            }
            return null;
        }
    }
}
