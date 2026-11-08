using AutoMapper;
using AIKnowledgeAssistant.Application.DTOs;
using AIKnowledgeAssistant.Domain.Entities;

namespace AIKnowledgeAssistant.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Document, DocumentDto>()
            .ForMember(dest => dest.ChunkCount, opt => opt.MapFrom(src => src.Chunks.Count))
            .ForMember(dest => dest.FormattedSize, opt => opt.MapFrom(src => FormatBytes(src.FileSizeBytes)));

        CreateMap<UsageLog, UsageLogDto>()
            .ForMember(dest => dest.Intent, opt => opt.MapFrom(src => src.Intent.ToString()))
            .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => 
                src.Answer.Length > 180 ? src.Answer.Substring(0, 180) + "..." : src.Answer));

        CreateMap<DocumentChunk, RetrievedChunk>()
            .ForMember(dest => dest.ChunkId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.DocumentName, opt => opt.MapFrom(src => src.Document != null ? src.Document.OriginalFileName : "Document.pdf"));
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int i = 0;
        double dblBytes = bytes;
        while (dblBytes >= 1024 && i < suffixes.Length - 1)
        {
            dblBytes /= 1024;
            i++;
        }
        return $"{dblBytes:0.##} {suffixes[i]}";
    }
}
