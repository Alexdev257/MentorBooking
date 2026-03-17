using AIService.Application.DTOs.Transcripts;
using AIService.Domain.Entities;
using AutoMapper;

namespace AIService.Application.Common;

public class AIServiceMappingProfile : Profile
{
    public AIServiceMappingProfile()
    {
        CreateMap<AudioTranscript, TranscriptDetailDto>();
        CreateMap<AudioTranscript, TranscriptListItemDto>();
        CreateMap<AudioTranscriptSegment, TranscriptSegmentDto>();
    }
}
