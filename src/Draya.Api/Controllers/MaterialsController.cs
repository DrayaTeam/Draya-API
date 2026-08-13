using Draya.Application.Materials;
using Draya.Application.Materials.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Draya.Api.Controllers;

[Route("api/v1")]
[ApiController]
[Authorize]
public class MaterialsController : ControllerBase
{
    private readonly IMaterialService _materialService;

    public MaterialsController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    [HttpPost("classrooms/{classroomId:guid}/materials")]
    [Authorize(Roles = "Teacher")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLessonMaterial(Guid classroomId, [FromForm] string title, [FromForm] string materialType, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File is required.");

        var material = await _materialService.UploadLessonMaterialAsync(
            classroomId, 
            title, 
            materialType, 
            file.OpenReadStream(), 
            file.FileName, 
            file.ContentType);

        return Accepted(material); // 202 Accepted
    }

    [HttpGet("classrooms/{classroomId:guid}/materials")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> GetClassroomMaterials(Guid classroomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var materials = await _materialService.GetClassroomMaterialsAsync(classroomId, page, pageSize);
        return Ok(materials);
    }

    [HttpGet("materials/{materialId:guid}")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> GetMaterialDetail(Guid materialId)
    {
        var material = await _materialService.GetMaterialDetailAsync(materialId);
        return Ok(material);
    }

    [HttpPost("materials/{materialId:guid}/versions")]
    [Authorize(Roles = "Teacher")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadNewMaterialVersion(Guid materialId, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("File is required.");

        var version = await _materialService.UploadNewMaterialVersionAsync(
            materialId, 
            file.OpenReadStream(), 
            file.FileName, 
            file.ContentType);

        return Accepted(version); // 202 Accepted
    }

    [HttpGet("materials/{materialId:guid}/versions")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetMaterialVersionHistory(Guid materialId)
    {
        var versions = await _materialService.GetMaterialVersionHistoryAsync(materialId);
        return Ok(versions);
    }

    [HttpGet("materials/{materialId:guid}/versions/{versionId:guid}/status")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetVersionParseStatus(Guid materialId, Guid versionId)
    {
        var status = await _materialService.GetVersionParseStatusAsync(materialId, versionId);
        return Ok(status);
    }

    [HttpDelete("materials/{materialId:guid}")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> SoftDeleteMaterial(Guid materialId)
    {
        await _materialService.DeleteMaterialAsync(materialId);
        return NoContent(); // 204 No Content
    }

    [HttpGet("materials/{materialId:guid}/stream")]
    [Authorize(Roles = "Teacher,Student")]
    public async Task<IActionResult> GetVideoStreamingUrl(Guid materialId)
    {
        var streamInfo = await _materialService.GetVideoStreamingUrlAsync(materialId);
        return Ok(streamInfo);
    }
}
