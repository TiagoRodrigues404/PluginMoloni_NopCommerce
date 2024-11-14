namespace Nop.Plugin.Misc.Moloni.Services.Cryptography
{
    /// <summary>
    /// Interface para a classe Cryptography, define os métodos de encriptação e desencriptação de texto.
    /// </summary>
    public interface ICryptography
    {
        /// <summary>
        /// Método para encriptar um texto simples.
        /// </summary>
        /// <param name="plainText">Texto em formato simples (não encriptado) que será encriptado.</param>
        /// <returns>Retorna uma string encriptada em Base64.</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Método para desencriptar um texto encriptado.
        /// </summary>
        /// <param name="cipherText">Texto encriptado em Base64 que será desencriptado.</param>
        /// <returns>Retorna o texto desencriptado em formato simples.</returns>
        string Decrypt(string cipherText);
    }
}
