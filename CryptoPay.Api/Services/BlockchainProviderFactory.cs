namespace CryptoPay.Api.Services;

public class BlockchainProviderFactory
{
    private readonly IEnumerable<IBlockchainProvider> _providers;

    public BlockchainProviderFactory(IEnumerable<IBlockchainProvider> providers)
    {
        _providers = providers;
    }

    public IBlockchainProvider GetProvider(string network)
    {
        var provider = _providers.FirstOrDefault(p => p.SupportsNetwork(network));
        if (provider == null)
        {
            throw new NotSupportedException($"Network {network} is not supported");
        }
        return provider;
    }
}
