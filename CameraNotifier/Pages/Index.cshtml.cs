using CameraNotifier.Services.WatchService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace CameraNotifier.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWatchService _watchService;

        public IndexModel(ILogger<IndexModel> logger, IWatchService watchService)
        {
            _logger = logger;
            _watchService = watchService;
        }

        public void OnGet()
        {
            (Successful, Failed) = _watchService.GetStats();
        }

        [BindProperty] public int Successful { get; set; }
        [BindProperty] public int Failed { get; set; }
    }
}
