using Draya.Application.Materials.RAG;
using Draya.Domain.Materials;
using Microsoft.Extensions.DependencyInjection;

namespace Draya.Infrastructure.Materials.RAG.Extractors;

public class ContentExtractorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ContentExtractorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IContentExtractor GetExtractor(MaterialType type)
    {
        var extractors = _serviceProvider.GetServices<IContentExtractor>();
        
        var extractor = extractors.FirstOrDefault(e => e.SupportedType == type);
        
        if (extractor == null)
        {
            throw new NotSupportedException($"No content extractor found for MaterialType: {type}");
        }

        return extractor;
    }
}
