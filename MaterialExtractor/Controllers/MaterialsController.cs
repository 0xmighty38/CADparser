using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MaterialExtractor.Models;
using MaterialExtractor.Services;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace MaterialExtractor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly DxfParserService _dxfParserService;

        public MaterialsController(DxfParserService dxfParserService)
        {
            _dxfParserService = dxfParserService;
        }

        [HttpPost("extract")]
        public IActionResult ExtractMaterials(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            string filePath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            var materials = _dxfParserService.ParseDxfFile(filePath);
            return Ok(materials);
        }
    }
}