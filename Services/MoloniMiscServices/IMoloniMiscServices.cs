using Nop.Plugin.Misc.Moloni.Models;

namespace Nop.Plugin.Misc.Moloni.Services.MoloniMiscServices
{
    public interface IMoloniMiscServices
    {
        /// <summary>
        /// Obtém todas as unidades de medida disponíveis no Moloni.
        /// Este método retorna uma lista de unidades de medida configuradas no Moloni, como "unidade", "quilograma", entre outras.
        /// </summary>
        /// <returns>Lista de unidades de medida ou null se ocorrer um erro.</returns>
        Task<List<MeasurementUnit>?> GetAllMeasurementUnits();

        /// <summary>
        /// Obtém todas as propriedades de produtos configuradas no Moloni.
        /// Esse método é utilizado para listar propriedades customizadas associadas aos produtos, como "cor", "tamanho", entre outras.
        /// </summary>
        /// <returns>Lista de propriedades de produtos ou null se ocorrer um erro.</returns>
        Task<List<Properties>> GetPropertiesProducts();

        /// <summary>
        /// Cria uma nova propriedade de produto no Moloni.
        /// Esse método permite criar propriedades adicionais para produtos, facilitando a organização e detalhamento de informações.
        /// </summary>
        /// <param name="name">Nome da nova propriedade de produto.</param>
        /// <returns>ID da nova propriedade criada ou -1 se a criação falhar.</returns>
        Task<int> CreateProductProperty(string name);

        /// <summary>
        /// Obtém a lista de todos os países disponíveis no Moloni.
        /// Esse método é útil para configurações regionais, como endereços de entrega e informações de país de origem.
        /// </summary>
        /// <returns>Lista de países ou null se ocorrer um erro.</returns>
        Task<List<Country>?> GetCountries();

        /// <summary>
        /// Obtém a lista de todas as moedas disponíveis no Moloni.
        /// Esse método retorna todas as moedas configuradas, permitindo o uso de várias moedas em operações financeiras.
        /// </summary>
        /// <returns>Lista de moedas ou null se ocorrer um erro.</returns>
        Task<List<Currency>> GetCurrencies();
    }
}
