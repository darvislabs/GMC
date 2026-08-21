using GMC.BL.GMC;
using GMC.Helper;
using GMC.Models.GMC;
using Microsoft.AspNetCore.Mvc;

namespace GMC.Controllers.GMC
{
    [RoleAuth("Admin")]
    public class MasterListController : Controller
    {
        private readonly IMasterListBL _bl;
        public MasterListController(IMasterListBL bl) => _bl = bl;

        // ===== Company =====

        public async Task<IActionResult> Company()
        {
            ViewData["Title"] = "Insurance Company Master";
            var vm = new MasterListIndexModel
            {
                Title          = "Insurance Company Master",
                AddPlaceholder = "New company name…",
                ItemLabel      = "Company Name",
                AddAction      = nameof(AddCompany),
                EditAction     = nameof(EditCompany),
                DeleteAction   = nameof(DeleteCompany),
                Items          = await _bl.GetCompaniesAsync()
            };
            return View("MasterListIndex", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCompany(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                TempData["Error"] = "Name cannot be empty.";
            else if (!await _bl.AddCompanyAsync(name.Trim()))
                TempData["Error"] = "Could not save — it may already exist.";
            else
                TempData["Success"] = $"'{name.Trim()}' added.";

            return RedirectToAction(nameof(Company));
        }

        [HttpGet]
        public IActionResult EditCompany(string name)
        {
            ViewData["Title"] = "Edit Company";
            var vm = new EditMasterItemModel
            {
                OldName      = name ?? string.Empty,
                NewName      = name ?? string.Empty,
                Title        = "Edit Insurance Company",
                SaveAction   = nameof(EditCompanySave),
                CancelAction = nameof(Company)
            };
            return View("MasterListEdit", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCompanySave(EditMasterItemModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.NewName))
            {
                ModelState.AddModelError(nameof(vm.NewName), "Name cannot be empty.");
                vm.Title = "Edit Insurance Company";
                vm.SaveAction = nameof(EditCompanySave);
                vm.CancelAction = nameof(Company);
                return View("MasterListEdit", vm);
            }
            await _bl.UpdateCompanyAsync(vm.OldName, vm.NewName.Trim());
            TempData["Success"] = $"Updated to '{vm.NewName.Trim()}'.";
            return RedirectToAction(nameof(Company));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCompany(string name)
        {
            await _bl.DeleteCompanyAsync(name);
            TempData["Success"] = $"'{name}' removed.";
            return RedirectToAction(nameof(Company));
        }

        // ===== TPA =====

        public async Task<IActionResult> TPA()
        {
            ViewData["Title"] = "TPA Master";
            var vm = new MasterListIndexModel
            {
                Title          = "TPA Master",
                AddPlaceholder = "New TPA name…",
                ItemLabel      = "TPA Name",
                AddAction      = nameof(AddTpa),
                EditAction     = nameof(EditTpa),
                DeleteAction   = nameof(DeleteTpa),
                Items          = await _bl.GetTpasAsync()
            };
            return View("MasterListIndex", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTpa(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                TempData["Error"] = "Name cannot be empty.";
            else if (!await _bl.AddTpaAsync(name.Trim()))
                TempData["Error"] = "Could not save — it may already exist.";
            else
                TempData["Success"] = $"'{name.Trim()}' added.";

            return RedirectToAction(nameof(TPA));
        }

        [HttpGet]
        public IActionResult EditTpa(string name)
        {
            ViewData["Title"] = "Edit TPA";
            var vm = new EditMasterItemModel
            {
                OldName      = name ?? string.Empty,
                NewName      = name ?? string.Empty,
                Title        = "Edit TPA",
                SaveAction   = nameof(EditTpaSave),
                CancelAction = nameof(TPA)
            };
            return View("MasterListEdit", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTpaSave(EditMasterItemModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.NewName))
            {
                ModelState.AddModelError(nameof(vm.NewName), "Name cannot be empty.");
                vm.Title = "Edit TPA";
                vm.SaveAction = nameof(EditTpaSave);
                vm.CancelAction = nameof(TPA);
                return View("MasterListEdit", vm);
            }
            await _bl.UpdateTpaAsync(vm.OldName, vm.NewName.Trim());
            TempData["Success"] = $"Updated to '{vm.NewName.Trim()}'.";
            return RedirectToAction(nameof(TPA));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTpa(string name)
        {
            await _bl.DeleteTpaAsync(name);
            TempData["Success"] = $"'{name}' removed.";
            return RedirectToAction(nameof(TPA));
        }

        // ===== Industry =====

        public async Task<IActionResult> Industry()
        {
            ViewData["Title"] = "Nature of Industry Master";
            var vm = new MasterListIndexModel
            {
                Title          = "Nature of Industry Master",
                AddPlaceholder = "New industry…",
                ItemLabel      = "Nature of Industry",
                AddAction      = nameof(AddIndustry),
                EditAction     = nameof(EditIndustry),
                DeleteAction   = nameof(DeleteIndustry),
                Items          = await _bl.GetIndustriesAsync()
            };
            return View("MasterListIndex", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIndustry(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                TempData["Error"] = "Name cannot be empty.";
            else if (!await _bl.AddIndustryAsync(name.Trim()))
                TempData["Error"] = "Could not save — it may already exist.";
            else
                TempData["Success"] = $"'{name.Trim()}' added.";

            return RedirectToAction(nameof(Industry));
        }

        [HttpGet]
        public IActionResult EditIndustry(string name)
        {
            ViewData["Title"] = "Edit Nature of Industry";
            var vm = new EditMasterItemModel
            {
                OldName      = name ?? string.Empty,
                NewName      = name ?? string.Empty,
                Title        = "Edit Nature of Industry",
                SaveAction   = nameof(EditIndustrySave),
                CancelAction = nameof(Industry)
            };
            return View("MasterListEdit", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditIndustrySave(EditMasterItemModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.NewName))
            {
                ModelState.AddModelError(nameof(vm.NewName), "Name cannot be empty.");
                vm.Title = "Edit Nature of Industry";
                vm.SaveAction = nameof(EditIndustrySave);
                vm.CancelAction = nameof(Industry);
                return View("MasterListEdit", vm);
            }
            await _bl.UpdateIndustryAsync(vm.OldName, vm.NewName.Trim());
            TempData["Success"] = $"Updated to '{vm.NewName.Trim()}'.";
            return RedirectToAction(nameof(Industry));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIndustry(string name)
        {
            await _bl.DeleteIndustryAsync(name);
            TempData["Success"] = $"'{name}' removed.";
            return RedirectToAction(nameof(Industry));
        }

        // ===== Financial Year =====

        public async Task<IActionResult> FinancialYear()
        {
            ViewData["Title"] = "Financial Year Master";
            var vm = new MasterListIndexModel
            {
                Title          = "Financial Year Master",
                AddPlaceholder = "YYYY-YY (for example 2025-26)",
                ItemLabel      = "Financial Year",
                AddAction      = nameof(AddFinancialYear),
                EditAction     = nameof(EditFinancialYear),
                DeleteAction   = nameof(DeleteFinancialYear),
                Items          = await _bl.GetFinancialYearsAsync()
            };
            return View("MasterListIndex", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFinancialYear(string name)
        {
            name = (name ?? string.Empty).Trim();
            if (!IsValidFinancialYear(name))
                TempData["Error"] = "Enter a valid consecutive financial year in YYYY-YY format, for example 2025-26.";
            else if (!await _bl.AddFinancialYearAsync(name))
                TempData["Error"] = "Could not save — the financial year may already exist.";
            else
                TempData["Success"] = $"Financial year '{name}' added.";

            return RedirectToAction(nameof(FinancialYear));
        }

        [HttpGet]
        public IActionResult EditFinancialYear(string name)
        {
            ViewData["Title"] = "Edit Financial Year";
            return View("MasterListEdit", new EditMasterItemModel
            {
                OldName      = name ?? string.Empty,
                NewName      = name ?? string.Empty,
                Title        = "Edit Financial Year",
                SaveAction   = nameof(EditFinancialYearSave),
                CancelAction = nameof(FinancialYear)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFinancialYearSave(EditMasterItemModel vm)
        {
            vm.NewName = (vm.NewName ?? string.Empty).Trim();
            if (!IsValidFinancialYear(vm.NewName))
            {
                ModelState.AddModelError(nameof(vm.NewName),
                    "Enter a valid consecutive financial year in YYYY-YY format, for example 2025-26.");
                vm.Title = "Edit Financial Year";
                vm.SaveAction = nameof(EditFinancialYearSave);
                vm.CancelAction = nameof(FinancialYear);
                return View("MasterListEdit", vm);
            }

            if (!await _bl.UpdateFinancialYearAsync(vm.OldName, vm.NewName))
                TempData["Error"] = "Could not update the financial year.";
            else
                TempData["Success"] = $"Updated to '{vm.NewName}'.";
            return RedirectToAction(nameof(FinancialYear));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFinancialYear(string name)
        {
            if (!await _bl.DeleteFinancialYearAsync(name))
                TempData["Error"] = "Could not delete the financial year.";
            else
                TempData["Success"] = $"Financial year '{name}' removed.";
            return RedirectToAction(nameof(FinancialYear));
        }

        private static bool IsValidFinancialYear(string value)
        {
            if (value.Length != 7 || value[4] != '-'
                || !int.TryParse(value[..4], out var start)
                || !int.TryParse(value[5..], out var end))
                return false;

            return start is >= 2000 and <= 9998 && end == (start + 1) % 100;
        }
    }
}
