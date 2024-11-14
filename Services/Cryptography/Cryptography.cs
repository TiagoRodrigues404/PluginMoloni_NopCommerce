using Microsoft.Extensions.Options;
using Nop.Plugin.Misc.Moloni.Services.MoloniSettingsProviderService;
using System.Security.Cryptography;

namespace Nop.Plugin.Misc.Moloni.Services.Cryptography
{
    /// <summary>
    /// Classe responsável por realizar operações de criptografia, como encriptação e desencriptação de textos.
    /// Utiliza o algoritmo AES para garantir a segurança dos dados.
    /// </summary>
    public class Cryptography : ICryptography
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;
        private readonly IMoloniSettingsProvider _moloniSettings;

        /// <summary>
        /// Construtor da classe Cryptography.
        /// Inicializa as chaves de criptografia (_key) e vetor de inicialização (_iv) usando valores baseados em uma string codificada em Base64.
        /// </summary>
        public Cryptography(IMoloniSettingsProvider moloniSettings)
        {
            _moloniSettings = moloniSettings;

            MoloniSettings settings = Task.Run(() => moloniSettings.GetMoloniSettingsAsync()).Result;

            _key = Convert.FromBase64String(settings._key);
            _iv = Convert.FromBase64String(settings._iv);
        }

        /// <summary>
        /// Encripta um texto simples utilizando o algoritmo AES.
        /// </summary>
        /// <param name="plainText">Texto em formato simples (não encriptado) que será encriptado.</param>
        /// <returns>Retorna uma string encriptada em Base64.</returns>
        /// <exception cref="ArgumentNullException">Lança uma exceção se o texto simples fornecido for nulo ou vazio.</exception>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.IV = _iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        /// <summary>
        /// Desencripta uma string encriptada em Base64 utilizando o algoritmo AES.
        /// </summary>
        /// <param name="cipherText">Texto encriptado em Base64 que será desencriptado.</param>
        /// <returns>Retorna o texto desencriptado em formato simples.</returns>
        /// <exception cref="ArgumentNullException">Lança uma exceção se o texto encriptado fornecido for nulo ou vazio.</exception>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentNullException(nameof(cipherText));

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.IV = _iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }
}
