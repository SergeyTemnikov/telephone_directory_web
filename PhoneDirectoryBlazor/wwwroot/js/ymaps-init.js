// wwwroot/js/ymaps-init.js

window._yandexMaps = window._yandexMaps || {};

/**
 * Инициализация карты Яндекс.Карты API v2.1
 * @param {string} elementId - ID DOM-элемента для карты
 * @param {string} apiKey - API-ключ Яндекс
 * @param {number} lat - Широта центра карты
 * @param {number} lon - Долгота центра карты
 * @param {number} zoom - Уровень зума
 * @param {object} dotNetRef - .NET объект для обратных вызовов (опционально)
 */
window.initYandexMap = async function (elementId, apiKey, lat = 55.75, lon = 37.62, zoom = 10, dotNetRef = null) {
    if (!elementId) throw new Error("elementId required");

    // Загружаем API v2.1 один раз глобально
    if (!window._ymapsLoading) {
        window._ymapsLoading = new Promise((resolve, reject) => {
            // Если API уже загружен — сразу резолвим
            if (window.ymaps && window.ymaps.Map) {
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = `https://api-maps.yandex.ru/2.1/?apikey=${encodeURIComponent(apiKey)}&lang=ru_RU`;
            script.async = true;
            script.onload = () => {
                console.info("✅ Yandex Maps API v2.1 loaded");
                resolve();
            };
            script.onerror = (err) => {
                console.error("❌ Failed to load Yandex Maps API v2.1", err);
                reject(new Error("Failed to load Yandex Maps API v2.1"));
            };
            document.head.appendChild(script);
        });
    }

    // Ждём загрузки скрипта
    await window._ymapsLoading;

    // Ждём готовности API через ymaps.ready
    await new Promise(resolve => ymaps.ready(resolve));

    // Удаляем старую карту, если есть
    if (window._yandexMaps[elementId]) {
        try {
            window._yandexMaps[elementId].map.destroy();
        } catch (e) {
            console.warn("⚠️ Error destroying old map:", e);
        }
        delete window._yandexMaps[elementId];
    }

    // 🔹 Создаём карту: порядок координат [широта, долгота] = [lat, lon]
    const map = new ymaps.Map(elementId, {
        center: [lat, lon],
        zoom: zoom,
        controls: ['zoomControl', 'geolocationControl', 'searchControl']
    });

    // Сохраняем ссылку на карту и .NET реф для колбэков
    window._yandexMaps[elementId] = {
        map,
        marker: null,
        dotNetRef
    };

    console.info(`🗺️ Map initialized for #${elementId}`, { lat, lon, zoom });
    return true;
};

/**
 * Установить/обновить метку на карте
 * @param {string} elementId - ID элемента карты
 * @param {number} lat - Широта
 * @param {number} lon - Долгота
 * @param {object} options - Опции: { label, hint, balloon, url, target, onClickPayload }
 */
window.setYandexMarker = function (elementId, lat, lon, options = {}) {
    const entry = window._yandexMaps[elementId];
    if (!entry || !entry.map) {
        console.warn(`⚠️ No map found for elementId: ${elementId}`);
        return false;
    }

    const map = entry.map;

    // Удаляем старую метку, если есть
    if (entry.marker) {
        try {
            map.geoObjects.remove(entry.marker);
        } catch (e) {
            console.warn("⚠️ Error removing old marker:", e);
        }
        entry.marker = null;
    }

    // 🔹 Создаём метку: порядок [lat, lon] для API v2.1!
    const placemark = new ymaps.Placemark([lat, lon], {
        hintContent: options.hint || options.label || '',
        balloonContent: options.balloon || options.label || ''
    }, {
        preset: options.preset || 'islands#redCircleDotIcon',
        draggable: options.draggable || false
    });

    // Добавляем метку на карту
    map.geoObjects.add(placemark);
    entry.marker = placemark;

    // 🔹 Обработчик клика по метке
    if (options.url || options.onClickPayload || entry.dotNetRef) {
        placemark.events.add('click', function (e) {
            // 1. Открыть URL, если указан
            if (options.url) {
                window.open(options.url, options.target || '_blank');
                return;
            }

            // 2. Вызвать .NET метод, если есть dotNetRef
            if (entry.dotNetRef && typeof entry.dotNetRef.invokeMethodAsync === 'function') {
                entry.dotNetRef.invokeMethodAsync('OnMarkerClicked', options.onClickPayload || null)
                    .catch(err => console.error("❌ Error invoking .NET method:", err));
            }
        });
    }

    // 🔹 Центрируем карту и устанавливаем зум при необходимости
    if (options.zoom) {
        map.setZoom(options.zoom);
    }
    map.setCenter([lat, lon]);

    console.info(`📍 Marker set on #${elementId}`, { lat, lon, options });
    return true;
};

/**
 * Геокодирование адреса → координаты
 * @param {string} address - Адрес для геокодирования
 * @param {string} apiKey - API-ключ
 * @returns {Promise<{lat: number, lon: number} | null>}
 */
window.geocodeAddress = async function (address, apiKey) {
    if (!address || !apiKey) {
        console.error("❌ geocodeAddress: missing address or apiKey");
        return null;
    }

    try {
        const url = `https://geocode-maps.yandex.ru/1.x/?apikey=${encodeURIComponent(apiKey)}&format=json&geocode=${encodeURIComponent(address)}`;
        const response = await fetch(url);

        if (!response.ok) {
            console.error(`❌ Geocode failed: ${response.status} ${response.statusText}`);
            return null;
        }

        const json = await response.json();
        const pos = json?.response?.GeoObjectCollection?.featureMember?.[0]?.GeoObject?.Point?.pos;

        if (!pos) {
            console.warn("⚠️ No position found in geocode response");
            return null;
        }

        // 🔹 pos = "долгота широта" → парсим в {lat, lon}
        const [lon, lat] = pos.split(' ').map(Number);
        if (isNaN(lat) || isNaN(lon)) {
            console.error("❌ Invalid coordinates:", pos);
            return null;
        }

        console.info("✅ Geocode success:", { lat, lon });
        return { lat, lon };

    } catch (err) {
        console.error("❌ geocodeAddress error:", err);
        return null;
    }
};

/**
 * Удалить карту
 * @param {string} elementId - ID элемента карты
 */
window.destroyYandexMap = function (elementId) {
    const entry = window._yandexMaps[elementId];
    if (!entry || !entry.map) return;

    try {
        entry.map.destroy();
        console.info(`🗑️ Map destroyed for #${elementId}`);
    } catch (e) {
        console.warn("⚠️ Error destroying map:", e);
    }
    delete window._yandexMaps[elementId];
};

/**
 * Прокрутка страницы к элементу по ID (для навигации)
 * @param {string} id - ID элемента
 */
window.scrollToElementById = function (id) {
    const el = document.getElementById(id);
    if (!el) return;

    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    el.classList.add('highlight-temp');
    setTimeout(() => el.classList.remove('highlight-temp'), 2000);
};