window.plantCare = (function () {
    const themeKey = "plantcare-theme";

    function setTheme(isDark) {
        document.body.classList.toggle("dark-mode", isDark);
        localStorage.setItem(themeKey, isDark ? "dark" : "light");
    }

    return {
        applySavedTheme: () => {
            const stored = localStorage.getItem(themeKey);
            const prefersDark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
            const isDark = stored ? stored === "dark" : prefersDark;
            setTheme(isDark);
            return isDark;
        },
        toggleTheme: () => {
            const isDark = !(document.body.classList.contains("dark-mode"));
            setTheme(isDark);
            return isDark;
        },
        getCurrentPosition: () => {
            if (!navigator.geolocation) {
                throw new Error("Geolocalización no soportada.");
            }

            return new Promise((resolve, reject) => {
                navigator.geolocation.getCurrentPosition(
                    (position) => resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    }),
                    (error) => reject(error),
                    { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 });
            });
        },
        scrollToBottom: (element) => {
            if (!element) {
                return;
            }
            requestAnimationFrame(() => {
                element.scrollTo({ top: element.scrollHeight, behavior: "smooth" });
            });
        }
    };
})();

window.plantCareMap = (function () {
    const instances = new WeakMap();

    function init(element, dotNetRef, lat, lon) {
        if (!element || !window.L) {
            return false;
        }

        try {
            const iconPath = "lib/leaflet/images/";
            L.Icon.Default.mergeOptions({
                iconUrl: `${iconPath}marker-icon.png`,
                iconRetinaUrl: `${iconPath}marker-icon-2x.png`,
                shadowUrl: `${iconPath}marker-shadow.png`
            });
        } catch {
            // ignore icon errors
        }

        const center = [lat ?? 0, lon ?? 0];
        const map = L.map(element).setView(center, 11);
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 18,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(map);

        const marker = L.marker(center, { draggable: true }).addTo(map);

        function report(latlng) {
            marker.setLatLng(latlng);
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("OnMapLocationChanged", latlng.lat, latlng.lng);
            }
        }

        marker.on("dragend", () => report(marker.getLatLng()));
        map.on("click", (e) => report(e.latlng));

        setTimeout(() => map.invalidateSize(), 200);
        instances.set(element, { map, marker });
        return true;
    }

    function update(element, lat, lon) {
        const inst = instances.get(element);
        if (!inst) {
            return;
        }
        const latLng = L.latLng(lat ?? 0, lon ?? 0);
        inst.marker.setLatLng(latLng);
        inst.map.setView(latLng, inst.map.getZoom());
    }

    function dispose(element) {
        const inst = instances.get(element);
        if (inst) {
            inst.map.remove();
            instances.delete(element);
        }
    }

    return {
        init,
        update,
        dispose
    };
})();

window.plantCareZones = (function () {
    const key = "plantcare-zones";

    function read() {
        try {
            const raw = localStorage.getItem(key);
            if (!raw) return [];
            const parsed = JSON.parse(raw);
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    }

    function write(zones) {
        localStorage.setItem(key, JSON.stringify(zones));
    }

    return {
        getAll: () => read(),
        add: (zone) => {
            const zones = read();
            if (!zones.find(z => z.toLowerCase() === zone.toLowerCase())) {
                zones.push(zone);
                write(zones);
            }
        },
        remove: (zone) => {
            let zones = read();
            zones = zones.filter(z => z.toLowerCase() !== zone.toLowerCase());
            write(zones);
        }
    };
})();
