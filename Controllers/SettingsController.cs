using Decisionman.Data;
using Decisionman.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Decisionman.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public SettingsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public class SetSettingRequest
        {
            public string Key { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        [HttpGet("{key}")]
        public async Task<IActionResult> GetSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("Key must be provided.");

            var setting = await _dbContext.SystemSettings.FindAsync(key);
            if (setting == null)
                return NotFound($"Setting '{key}' not found.");

            return Ok(new { setting.Key, setting.Value });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSetting([FromBody] SetSettingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
                return BadRequest("Key cannot be empty.");

            var setting = await _dbContext.SystemSettings.FindAsync(request.Key);
            if (setting == null)
            {
                setting = new SystemSetting { Key = request.Key, Value = request.Value };
                _dbContext.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = request.Value;
                _dbContext.SystemSettings.Update(setting);
            }

            await _dbContext.SaveChangesAsync();
            return Ok(new { setting.Key, setting.Value });
        }
    }
}
