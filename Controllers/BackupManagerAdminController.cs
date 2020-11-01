using System.Web.Mvc;
using Alexr03.Common.TCAdmin.Extensions;
using Alexr03.Common.TCAdmin.Objects;
using Alexr03.Common.Web.Extensions;
using TCAdmin.SDK.Web.MVC.Controllers;
using TCAdminBackupManager.Models.Objects;

namespace TCAdminBackupManager.Controllers
{
    public class BackupManagerAdminController : BaseController
    {
        public ActionResult Index()
        {
            return View();
        }

        [ParentAction("Admin")]
        public ActionResult Configure(int id)
        {
            var provider = DynamicTypeBase.GetCurrent<BackupProvider>();
            return provider.GetConfigurationResultView(this);
        }
        
        [ParentAction("Admin")]
        [HttpPost]
        public ActionResult Configure(int id, FormCollection model)
        {
            var provider = DynamicTypeBase.GetCurrent<BackupProvider>();
            provider.UpdateConfigurationFromCollection(model, ControllerContext);
            return this.SendSuccess("Updated configuration");
        }
    }
}