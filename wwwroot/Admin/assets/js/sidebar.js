(function () {
    var STORAGE_KEY = "admin.sidebar.state";
    var DESKTOP_MEDIA = window.matchMedia("(min-width: 992px)");

    function normalizePath(path) {
        if (!path) {
            return "/";
        }

        var cleaned = path.split("?")[0].split("#")[0].trim().toLowerCase();
        if (!cleaned) {
            return "/";
        }

        if (cleaned.length > 1 && cleaned.endsWith("/")) {
            cleaned = cleaned.slice(0, -1);
        }

        return cleaned;
    }

    function splitRoutes(value) {
        if (!value) {
            return [];
        }

        return value
            .split(",")
            .map(function (route) { return normalizePath(route); })
            .filter(Boolean);
    }

    function pathMatches(currentPath, candidatePath) {
        if (!candidatePath || candidatePath === "/") {
            return currentPath === "/";
        }

        return currentPath === candidatePath || currentPath.indexOf(candidatePath + "/") === 0;
    }

    function closeElementSubmenu(item) {
        var trigger = item.querySelector("[data-sidebar-submenu-trigger]");
        if (!trigger) {
            return;
        }

        item.classList.remove("submenu-open");
        trigger.setAttribute("aria-expanded", "false");
    }

    function openElementSubmenu(item) {
        var trigger = item.querySelector("[data-sidebar-submenu-trigger]");
        if (!trigger) {
            return;
        }

        item.classList.add("submenu-open");
        trigger.setAttribute("aria-expanded", "true");
    }

    function initSidebar() {
        var wrapper = document.getElementById("adminLayout");
        var sidebar = document.getElementById("adminSidebar");

        if (!wrapper || !sidebar) {
            return;
        }

        var desktopToggle = document.querySelector("[data-sidebar-desktop-toggle]");
        var mobileToggles = document.querySelectorAll("[data-sidebar-mobile-toggle]");
        var overlay = document.querySelector("[data-sidebar-overlay]");
        var submenuTriggers = sidebar.querySelectorAll("[data-sidebar-submenu-trigger]");
        var navItems = sidebar.querySelectorAll(".admin-sidebar__item");
        var navLinks = sidebar.querySelectorAll("[data-sidebar-link]");

        function isDesktop() {
            return DESKTOP_MEDIA.matches;
        }

        function getSavedDesktopState() {
            var saved = window.localStorage.getItem(STORAGE_KEY);
            return saved === "collapsed" ? "collapsed" : "expanded";
        }

        function saveDesktopState(state) {
            window.localStorage.setItem(STORAGE_KEY, state);
        }

        function closeMobileSidebar() {
            wrapper.classList.remove("sidebar-mobile-open");
            document.body.classList.remove("sidebar-mobile-open");
        }

        function openMobileSidebar() {
            wrapper.classList.add("sidebar-mobile-open");
            document.body.classList.add("sidebar-mobile-open");
        }

        function closeHoverPreview() {
            wrapper.classList.remove("sidebar-collapsed-hover");
        }

        function closeAllSubmenus(exceptItem) {
            navItems.forEach(function (item) {
                if (item !== exceptItem) {
                    closeElementSubmenu(item);
                }
            });
        }

        function isCollapsedDesktop() {
            return isDesktop() && wrapper.classList.contains("sidebar-collapsed");
        }

        function applyDesktopState(state) {
            var collapsed = state === "collapsed";

            wrapper.classList.toggle("sidebar-collapsed", collapsed);
            wrapper.classList.toggle("sidebar-expanded", !collapsed);

            if (desktopToggle) {
                desktopToggle.setAttribute("aria-label", collapsed ? "Mo rong sidebar" : "Thu gon sidebar");
            }

            if (!collapsed) {
                closeHoverPreview();
            }
        }

        function syncForViewport() {
            if (isDesktop()) {
                closeMobileSidebar();
                applyDesktopState(getSavedDesktopState());
                return;
            }

            wrapper.classList.remove(
                "sidebar-collapsed",
                "sidebar-expanded",
                "sidebar-collapsed-hover"
            );
            closeAllSubmenus();
        }

        function setActiveNavigation() {
            var currentPath = normalizePath(window.location.pathname);
            var activeItem = null;

            navLinks.forEach(function (link) {
                link.classList.remove("active");
                link.removeAttribute("aria-current");
            });

            navItems.forEach(function (item) {
                item.classList.remove("active");
                closeElementSubmenu(item);

                var exactRoutes = splitRoutes(item.getAttribute("data-route-exact"));
                var prefixRoutes = splitRoutes(item.getAttribute("data-route-prefixes"));
                var matched = exactRoutes.some(function (route) { return route === currentPath; });

                if (!matched) {
                    matched = prefixRoutes.some(function (route) { return pathMatches(currentPath, route); });
                }

                var childLinks = item.querySelectorAll(".admin-sidebar__sublink[href]");
                var activeChild = null;

                childLinks.forEach(function (childLink) {
                    var childPath = normalizePath(childLink.getAttribute("href"));
                    if (!activeChild && pathMatches(currentPath, childPath)) {
                        activeChild = childLink;
                    }
                });

                if (!matched) {
                    var directLink = item.querySelector(".admin-sidebar__link[href]");
                    if (directLink) {
                        matched = pathMatches(currentPath, normalizePath(directLink.getAttribute("href")));
                    }
                }

                if (activeChild) {
                    matched = true;
                    activeChild.classList.add("active");
                    activeChild.setAttribute("aria-current", "page");
                }

                if (matched) {
                    item.classList.add("active");
                    activeItem = item;

                    var directActiveLink = item.querySelector(".admin-sidebar__link[href]");
                    if (directActiveLink && !activeChild) {
                        directActiveLink.setAttribute("aria-current", "page");
                    }

                    if (item.classList.contains("admin-sidebar__item--has-submenu")) {
                        openElementSubmenu(item);
                    }
                }
            });

            if (activeItem && activeItem.classList.contains("admin-sidebar__item--has-submenu")) {
                closeAllSubmenus(activeItem);
                openElementSubmenu(activeItem);
            }
        }

        if (desktopToggle) {
            desktopToggle.addEventListener("click", function () {
                if (!isDesktop()) {
                    return;
                }

                var nextState = wrapper.classList.contains("sidebar-collapsed") ? "expanded" : "collapsed";
                applyDesktopState(nextState);
                saveDesktopState(nextState);
            });
        }

        mobileToggles.forEach(function (toggle) {
            toggle.addEventListener("click", function () {
                if (isDesktop()) {
                    return;
                }

                if (wrapper.classList.contains("sidebar-mobile-open")) {
                    closeMobileSidebar();
                    return;
                }

                openMobileSidebar();
            });
        });

        if (overlay) {
            overlay.addEventListener("click", closeMobileSidebar);
        }

        sidebar.addEventListener("mouseenter", function () {
            if (isCollapsedDesktop()) {
                wrapper.classList.add("sidebar-collapsed-hover");
            }
        });

        sidebar.addEventListener("mouseleave", function () {
            closeHoverPreview();
        });

        sidebar.addEventListener("focusin", function () {
            if (isCollapsedDesktop()) {
                wrapper.classList.add("sidebar-collapsed-hover");
            }
        });

        sidebar.addEventListener("focusout", function () {
            window.requestAnimationFrame(function () {
                if (!sidebar.contains(document.activeElement)) {
                    closeHoverPreview();
                }
            });
        });

        submenuTriggers.forEach(function (trigger) {
            trigger.addEventListener("click", function () {
                var item = trigger.closest(".admin-sidebar__item");
                if (!item) {
                    return;
                }

                var willOpen = !item.classList.contains("submenu-open");
                closeAllSubmenus(willOpen ? item : null);

                if (willOpen) {
                    openElementSubmenu(item);
                } else {
                    closeElementSubmenu(item);
                }
            });

            trigger.addEventListener("keydown", function (event) {
                var item = trigger.closest(".admin-sidebar__item");
                if (!item) {
                    return;
                }

                if (event.key === "ArrowRight") {
                    openElementSubmenu(item);
                }

                if (event.key === "ArrowLeft") {
                    closeElementSubmenu(item);
                }

                if (event.key === "Escape") {
                    closeElementSubmenu(item);
                    closeMobileSidebar();
                    trigger.blur();
                }
            });
        });

        navLinks.forEach(function (link) {
            link.addEventListener("click", function () {
                if (!isDesktop()) {
                    closeMobileSidebar();
                }
            });
        });

        document.addEventListener("keydown", function (event) {
            if (event.key !== "Escape") {
                return;
            }

            closeHoverPreview();
            closeMobileSidebar();
            closeAllSubmenus();
        });

        if (typeof DESKTOP_MEDIA.addEventListener === "function") {
            DESKTOP_MEDIA.addEventListener("change", syncForViewport);
        } else if (typeof DESKTOP_MEDIA.addListener === "function") {
            DESKTOP_MEDIA.addListener(syncForViewport);
        }

        syncForViewport();
        setActiveNavigation();
    }

    window.initSidebar = initSidebar;

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initSidebar);
    } else {
        initSidebar();
    }
})();
