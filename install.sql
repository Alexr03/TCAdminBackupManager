create table tcmodule_backupmanager_providers
(
    id                    int         not null
        primary key,
    name                  text        null,
    typeName              text        null,
    configurationModuleId varchar(36) not null,
    configurationId       int         not null,
    app_data              text        null
);

create table tcmodule_backupmanager_service_backups
(
    id         int         not null
        primary key,
    name       text        null,
    guid       varchar(36) null,
    serviceId  int         null,
    ownerId    int         null,
    providerId int         null,
    app_data   text        null
);

-- ---------------------------------------------------------------------------------------------------------------------

INSERT INTO tc_site_map (page_id, module_id, parent_page_id, parent_page_module_id, category_id, url, mvc_url,
                         controller, action, display_name, page_small_icon, panelbar_icon, show_in_sidebar, view_order,
                         required_permissions, menu_required_permissions, page_manager, page_search_provider,
                         cache_name)
VALUES (1, 'ad3d6004-3dce-414c-a246-bccd461ba853', 40, '07405876-e8c2-4b24-a774-4ef57f596384', 1, '/BackupManagerAdmin',
        '/BackupManagerAdmin', 'BackupManagerAdmin', 'Index', 'Configure Backup Providers',
        'MenuIcons/Base/ServerComponents24x24.png', 'MenuIcons/Base/ServerComponents16x16.png', 1, 100,
        '({07405876-e8c2-4b24-a774-4ef57f596384,0,8})', '({07405876-e8c2-4b24-a774-4ef57f596384,0,8})', null, null,
        null);
INSERT INTO tc_site_map (page_id, module_id, parent_page_id, parent_page_module_id, category_id, url, mvc_url,
                         controller, action, display_name, page_small_icon, panelbar_icon, show_in_sidebar, view_order,
                         required_permissions, menu_required_permissions, page_manager, page_search_provider,
                         cache_name)
VALUES (2, 'ad3d6004-3dce-414c-a246-bccd461ba853', 23, 'd3b2aa93-7e2b-4e0d-8080-67d14b2fa8a9', null,
        '/Service/BackupManager/$[SelectedService.ServiceId]', '/Service/BackupManager/$[SelectedService.ServiceId]',
        'BackupManager', 'Index', 'Backup Manager', 'MenuIcons/Base/ServerComponents24x24.png',
        'MenuIcons/Base/ServerComponents16x16.png', 0, 100, null, null, null, null, null);