using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using MaterialExtractor.Services;

namespace MaterialExtractor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly CadParserService _cadParserService;

        public MaterialsController(CadParserService cadParserService)
        {
            _cadParserService = cadParserService;
        }

        [HttpPost("extract")]
        public async Task<IActionResult> ExtractMaterials(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(uploadDir);

            var filePath = Path.Combine(uploadDir, file.FileName);
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (fileExtension != ".dxf" && fileExtension != ".dwg")
            {
                return BadRequest("Only DXF and DWG files are supported.");
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var materials = _cadParserService.ParseCadFile(filePath);

            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);

            return Ok(materials);
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("API is working!");
        }
    }
}