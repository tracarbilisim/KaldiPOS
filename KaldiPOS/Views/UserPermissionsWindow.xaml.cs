using KaldiPOS.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace KaldiPOS.Views;

public partial class UserPermissionsWindow : Window
{
    private readonly UserRecord _user;
    private readonly ObservableCollection<PermissionItemViewModel> _permissions;
    private readonly ObservableCollection<PermissionCategoryViewModel> _categories;

    public UserPermissionsWindow(UserRecord user)
    {
        InitializeComponent();

        _user = user;

        _permissions = new ObservableCollection<PermissionItemViewModel>(
            Database.GetUserPermissions(user.Id)
                .Select(permission => new PermissionItemViewModel(permission)));

        _categories = new ObservableCollection<PermissionCategoryViewModel>(
            _permissions
                .GroupBy(permission => permission.Category)
                .OrderBy(group => GetCategoryOrder(group.Key))
                .Select(group => new PermissionCategoryViewModel(
                    group.Key,
                    group.OrderBy(permission => permission.PermissionName)
                         .ToList())));

        SelectedUserNameText.Text = user.FullName;
        SelectedUserRoleText.Text = $"Rol: {user.Role}";
        CategoryCountText.Text = _categories.Count.ToString();

        CategoryColumnsItemsControl.ItemsSource = _categories;

        UpdatePermissionCounts();
    }

    private static int GetCategoryOrder(string category)
    {
        return category switch
        {
            "Masalar" => 1,
            "Sipariş" => 2,
            "Ödeme" => 3,
            "Menü" => 4,
            "Yönetim" => 5,
            _ => 99
        };
    }

    private void PermissionCheckBox_Click(
        object sender,
        RoutedEventArgs e)
    {
        UpdatePermissionCounts();
    }

    private void SelectGroupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is PermissionCategoryViewModel category)
        {
            SetCategoryPermissions(category, true);
        }
    }

    private void ClearGroupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is PermissionCategoryViewModel category)
        {
            SetCategoryPermissions(category, false);
        }
    }

    private void SetCategoryPermissions(
        PermissionCategoryViewModel category,
        bool isAllowed)
    {
        foreach (PermissionItemViewModel permission in category.Permissions)
            permission.IsAllowed = isAllowed;

        UpdatePermissionCounts();
    }

    private void SelectAllButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetAllPermissions(true);
    }

    private void ClearAllButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetAllPermissions(false);
    }

    private void SetAllPermissions(bool isAllowed)
    {
        foreach (PermissionItemViewModel permission in _permissions)
            permission.IsAllowed = isAllowed;

        UpdatePermissionCounts();
    }

    private void WaiterTemplateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ApplyTemplate(new[]
        {
            "Order.AddItem",
            "Order.IncreaseQuantity",
            "Order.DecreaseQuantity",
            "Order.Note",
            "Table.Open"
        });
    }

    private void CashierTemplateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ApplyTemplate(new[]
        {
            "Order.AddItem",
            "Order.RemoveItem",
            "Order.IncreaseQuantity",
            "Order.DecreaseQuantity",
            "Order.Note",
            "Order.Transfer",
            "Payment.Cash",
            "Payment.Card",
            "Payment.Mixed",
            "Payment.Close",
            "Table.Open",
            "Table.Merge",
            "Table.Split",
            "Tables.ViewOpenDuration",
            "Tables.ViewLastOrderDuration",
            "Tables.ViewOrderTotal",
            "Tables.ViewLiveStatus",
            "Menu.Reports"
        });
    }

    private void ManagerTemplateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ApplyTemplate(_permissions
            .Where(permission =>
                permission.PermissionKey != "Manage.Backup")
            .Select(permission => permission.PermissionKey));
    }

    private void AdministratorTemplateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetAllPermissions(true);
    }

    private void CustomTemplateButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetAllPermissions(false);
    }

    private void ApplyTemplate(IEnumerable<string> permissionKeys)
    {
        HashSet<string> allowedKeys =
            permissionKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (PermissionItemViewModel permission in _permissions)
        {
            permission.IsAllowed =
                allowedKeys.Contains(permission.PermissionKey);
        }

        UpdatePermissionCounts();
    }

    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            IEnumerable<PermissionRecord> records =
                _permissions.Select(permission =>
                    new PermissionRecord(
                        permission.PermissionKey,
                        permission.PermissionName,
                        permission.Category,
                        permission.IsAllowed));

            Database.SaveUserPermissions(_user.Id, records);

            DialogResult = true;
            Close();
        }
        catch (Exception exception)
        {
            KaldiMessageWindow.ShowWarning(
                this,
                "Yetkiler Kaydedilemedi",
                exception.Message);
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdatePermissionCounts()
    {
        SelectedPermissionCountText.Text =
            _permissions.Count(permission => permission.IsAllowed).ToString();

        foreach (PermissionCategoryViewModel category in _categories)
            category.RefreshSelectedCount();
    }
}

public sealed class PermissionItemViewModel : INotifyPropertyChanged
{
    private bool _isAllowed;

    public PermissionItemViewModel(PermissionRecord permission)
    {
        PermissionKey = permission.PermissionKey;
        PermissionName = permission.PermissionName;
        Category = permission.Category;
        _isAllowed = permission.IsAllowed;
    }

    public string PermissionKey { get; }
    public string PermissionName { get; }
    public string Category { get; }

    public bool IsAllowed
    {
        get => _isAllowed;
        set
        {
            if (_isAllowed == value)
                return;

            _isAllowed = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class PermissionCategoryViewModel : INotifyPropertyChanged
{
    private string _selectedCountText = string.Empty;

    public PermissionCategoryViewModel(
        string categoryName,
        IEnumerable<PermissionItemViewModel> permissions)
    {
        CategoryName = categoryName;
        Permissions = new ObservableCollection<PermissionItemViewModel>(
            permissions);

        RefreshSelectedCount();
    }

    public string CategoryName { get; }

    public ObservableCollection<PermissionItemViewModel> Permissions
    {
        get;
    }

    public string SelectedCountText
    {
        get => _selectedCountText;
        private set
        {
            if (_selectedCountText == value)
                return;

            _selectedCountText = value;
            OnPropertyChanged();
        }
    }

    public void RefreshSelectedCount()
    {
        int selectedCount =
            Permissions.Count(permission => permission.IsAllowed);

        SelectedCountText =
            $"{selectedCount} / {Permissions.Count} seçili";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
