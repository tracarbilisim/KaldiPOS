using KaldiPOS.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace KaldiPOS.Views;

public partial class UserPermissionsWindow : Window
{
    private readonly UserRecord _user;
    private readonly ObservableCollection<PermissionRecord> _permissions;
    private string _selectedCategory = string.Empty;

    public UserPermissionsWindow(UserRecord user)
    {
        InitializeComponent();

        _user = user;
        _permissions = new ObservableCollection<PermissionRecord>(
            Database.GetUserPermissions(user.Id));

        SelectedUserNameText.Text = user.FullName;
        SelectedUserRoleText.Text = $"Rol: {user.Role}";
        UserInfoText.Text = $"{user.FullName} kullanıcısının işlem yetkileri";

        LoadCategories();
        UpdatePermissionCount();
    }

    private void LoadCategories()
    {
        var categories = _permissions
            .Select(permission => permission.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(category => category)
            .ToList();

        CategoryListBox.ItemsSource = categories;

        if (categories.Count > 0)
            CategoryListBox.SelectedIndex = 0;
    }

    private void CategoryListBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (CategoryListBox.SelectedItem is not string category)
            return;

        _selectedCategory = category;
        CategoryTitleText.Text = category;
        RefreshPermissionList();
    }

    private void SearchTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        RefreshPermissionList();
    }

    private void RefreshPermissionList()
    {
        string searchText = SearchTextBox.Text.Trim();

        IEnumerable<PermissionRecord> filtered = _permissions;

        if (!string.IsNullOrWhiteSpace(_selectedCategory))
        {
            filtered = filtered.Where(permission =>
                permission.Category.Equals(
                    _selectedCategory,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(permission =>
                permission.PermissionName.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase) ||
                permission.PermissionKey.Contains(
                    searchText,
                    StringComparison.OrdinalIgnoreCase));
        }

        PermissionsItemsControl.ItemsSource = filtered.ToList();
    }

    private void SelectCategoryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetCategoryPermissions(true);
    }

    private void ClearCategoryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetCategoryPermissions(false);
    }

    private void SetCategoryPermissions(bool isAllowed)
    {
        for (int index = 0; index < _permissions.Count; index++)
        {
            PermissionRecord permission = _permissions[index];

            if (!permission.Category.Equals(
                    _selectedCategory,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            _permissions[index] = permission with
            {
                IsAllowed = isAllowed
            };
        }

        RefreshPermissionList();
        UpdatePermissionCount();
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
        for (int index = 0; index < _permissions.Count; index++)
        {
            _permissions[index] = _permissions[index] with
            {
                IsAllowed = isAllowed
            };
        }

        RefreshPermissionList();
        UpdatePermissionCount();
    }

    private void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Database.SaveUserPermissions(_user.Id, _permissions);

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

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdatePermissionCount()
    {
        SelectedPermissionCountText.Text =
            _permissions.Count(permission => permission.IsAllowed).ToString();
    }
}