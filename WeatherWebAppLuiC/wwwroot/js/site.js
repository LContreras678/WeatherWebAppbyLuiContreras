/**
 * This is the JS file which receives data from the user, gathers a list of cities and towns from the OpenWeather API, displays a suggestions box and allows the data to be selected.
 * The key code elements will be discussed here, due to the numerous elements utilised.
 * Firstly, DOMContentLoaded is used to initialise various elements in the page once parsed (MDN Contributors, 2025g), and it allows for addEventListener to be loaded. 
 * Hardware functionality such as mouse clicks are also made possible with addEventListener() method. (MDN Contributors, 2025j)
 * 
 * The addEventListener() method was added (MDN Contributors, 2025j), in order to set up a function and display the autocomplete or suggestions bar, for example.
 * In order to gather information for the OpenWeather API (OpenWeather, 2026), the querySelector() method is used to gather city or town name. (MDN Contributors, 2025f)
 * The getElementById() method is also visible here to gather other useful weather information, such as longitude and latitude. (MDN Contributors, 2025e)
 * Const is also declared throughout the entire JavaScript file, which are variables to manipulate elements, store information (such as city input, etc.), etc. (MDN Contributors, 2025c)
 * 
 * To allow for text to dynamically appear (allowing for a list of cities to appear), a createElement() method is used. (MDN Contributors, 2025d)
 * An accessibility feature is also available for keyboard users, which is enabled by using the keydown event. (MDN Contributors, 2025i)
 * The innerHTML property equals to an empty string, which allows for the suggestions to be cleared. (MDN Contributors, 2025h)
 * Since there is custom data being processed from an API, the dataset property is placed to allow for read and write access found in the HTMLElement. (MDN Contributors, 2025k)
 * Data such as longitude and latitude also exists here to be processed by the weather web app. 
 * 
 * The style is also made consistent by using the style property, allowing the appearance to match in the suggestions or autocomplete section. (MDN Contributors, 2025l)
 * The value property allows for the city input to be represented as string, which is also used for processing in the weather web app. (MDN Contributors, 2025m)
 * abortController allows for a web request to be aborted (MDN Contributors, 2025a), which can facilitate for the deletion of suggestions and the creation of new cities or places to appear. 
 * Using debounce in JavaScript is also helpful for features such as search box suggestions, ensuring that code is only triggered once per user input. (Polesny, 2021)
 * The feature or table which displays the weather around the world is created using an async function, separate from the autocomplete or suggestions bar. (MDN Contributors, 2025b)
 * This can allow for data to allow a function to be loaded in a subsequent manner. (Refsnes Data, 2026b)
 * 
 *
 */




const DEBUG = true;
// Leftover from a previous version when a popup modal was used (Refsnes Data, 2026a), but may be kept it for an upcoming feature.
document.addEventListener('DOMContentLoaded', () => {

    const modal = document.getElementById('popupModal');
    if (modal) {
        const closeBtn = modal.querySelector('button.close-button');
        modal.style.display = 'block';
        if (closeBtn) closeBtn.addEventListener('click', () => modal.style.display = 'none');
    }


    const cityInput = document.querySelector('input[name="City"]') ||
                                        document.getElementById('City') ||
                                        document.querySelector('input[type="text"]');
    const suggestionsBox = document.getElementById('city-suggestions');

    if (cityInput && suggestionsBox) {
        if (DEBUG) console.log('Autocomplete enabled');

        const DEBOUNCE_MS = 250;
        let debounceTimer = null;
        let abortController = null;
        let activeIndex = -1; 

        cityInput.setAttribute('aria-controls', 'city-suggestions');
        suggestionsBox.setAttribute('role', 'listbox');

        function clearSuggestions() {
            suggestionsBox.innerHTML = '';
            activeIndex = -1;
            cityInput.removeAttribute('aria-activedescendant');

            const latInput = document.getElementById('Lat');
            const lonInput = document.getElementById('Lon');
            if (latInput) latInput.value = '';
            if (lonInput) lonInput.value = '';
        }

        function updateActive(items) {
            items.forEach((it, i) => {
                const isActive = i === activeIndex;
                it.classList.toggle('autocomplete-active', isActive);
                it.setAttribute('aria-selected', isActive ? 'true' : 'false');

                if (isActive) {
                    cityInput.setAttribute('aria-activedescendant', it.id);
                    it.scrollIntoView({ block: 'nearest' });

                    try { it.focus(); } catch (e) { /* */ }
                }
            });
        }

        function selectItemByIndex(index) {
            const item = suggestionsBox.querySelector(`.autocomplete-item[data-index="${index}"]`);
            if (!item) return;
            cityInput.value = item.textContent;
            const lat = item.dataset.lat || '';
            const lon = item.dataset.lon || '';
            const latInput = document.getElementById('Lat');
            const lonInput = document.getElementById('Lon');
            if (latInput) latInput.value = lat;
            if (lonInput) lonInput.value = lon;
            clearSuggestions();
            cityInput.focus();
        }

        function renderSuggestions(cities) {
            suggestionsBox.innerHTML = '';
            if (!Array.isArray(cities) || cities.length === 0) return;
            const seen = new Set();
            const list = [];
            cities.forEach(c => {
                const key = `${c.name}|${c.state||''}|${c.country}|${c.lat}|${c.lon}`;
                if (seen.has(key)) return; seen.add(key);
                list.push(c);
            });

            list.forEach((city, idx) => {
                    const div = document.createElement('div');
                    div.className = 'autocomplete-item';
                    div.id = `city-suggestion-${idx}`;
                    div.dataset.index = idx;
                    div.dataset.lat = city.lat || '';
                    div.dataset.lon = city.lon || '';
                    div.setAttribute('role', 'option');
                    div.setAttribute('aria-selected', 'false');
                    div.tabIndex = -1; 
                    div.textContent = `${city.name}${city.state ? ', ' + city.state : ''}, ${city.country}`;

                    div.addEventListener('mouseover', () => { activeIndex = idx; updateActive(Array.from(suggestionsBox.children)); });
                    div.addEventListener('click', () => selectItemByIndex(idx));
                    div.addEventListener('keydown', (ev) => {
                        if (ev.key === 'Enter' || ev.key === ' ') { ev.preventDefault(); selectItemByIndex(idx); }
                    });
                    suggestionsBox.appendChild(div);
                });
        }

        cityInput.addEventListener('input', () => {
            const q = cityInput.value.trim();
            activeIndex = -1;
            cityInput.removeAttribute('aria-activedescendant');
            if (debounceTimer) clearTimeout(debounceTimer);
            if (abortController) { abortController.abort(); abortController = null; }
            if (q.length < 2) { clearSuggestions(); return; }
            debounceTimer = setTimeout(async () => {
                const url = `/Index?handler=Geocode&query=${encodeURIComponent(q)}`;
                abortController = new AbortController();
                try {
                    const resp = await fetch(url, { signal: abortController.signal });
                    if (!resp.ok) { clearSuggestions(); return; }
                    const cities = await resp.json();
                    renderSuggestions(cities);
                } catch (err) {
                    if (err.name === 'AbortError') return;
                    clearSuggestions();
                    if (DEBUG) console.error('Geocode fetch error', err);
                } finally {
                    abortController = null;
                }
            }, DEBOUNCE_MS);
        });

        cityInput.addEventListener('keydown', (e) => {
            const items = Array.from(suggestionsBox.querySelectorAll('.autocomplete-item'));
            if (!items.length) return;
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                activeIndex = Math.min(activeIndex + 1, items.length - 1);
                updateActive(items);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                activeIndex = Math.max(activeIndex - 1, 0);
                updateActive(items);
            } else if (e.key === 'Enter') {
                if (activeIndex >= 0) { e.preventDefault(); selectItemByIndex(activeIndex); }
            } else if (e.key === 'Escape') {
                clearSuggestions();
            }
        });

        // Allow keyboard navigation when focus is inside the suggestions box
        suggestionsBox.addEventListener('keydown', (e) => {
            const items = Array.from(suggestionsBox.querySelectorAll('.autocomplete-item'));
            if (!items.length) return;
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                activeIndex = Math.min(activeIndex + 1, items.length - 1);
                updateActive(items);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                activeIndex = Math.max(activeIndex - 1, 0);
                updateActive(items);
            } else if (e.key === 'Enter' || e.key === ' ') {
                if (activeIndex >= 0) { e.preventDefault(); selectItemByIndex(activeIndex); }
            } else if (e.key === 'Escape') {
                clearSuggestions(); cityInput.focus();
            }
        });

        // If the user clicks outside, the autocomplete/suggestions box will close
        document.addEventListener('click', (ev) => {
            if (!suggestionsBox.contains(ev.target) && ev.target !== cityInput) clearSuggestions();
        });
    }

    // Header sizing helper
    function setHeaderHeightVar() {
        const header = document.querySelector('header');
        if (!header) return;
        const h = Math.ceil(header.getBoundingClientRect().height);
        document.documentElement.style.setProperty('--site-header-height', h + 'px');
    }
    window.addEventListener('load', setHeaderHeightVar);
    window.addEventListener('resize', setHeaderHeightVar);
    setHeaderHeightVar();

    // Weather icons for fixed cities, data being loaded from OpenWeather API
    async function setWeatherIconsForCities() {
        const cities = [
            { name: 'New York', imgId: 'icon-newyork' },
            { name: 'London', imgId: 'icon-london' },
            { name: 'Tokyo', imgId: 'icon-japan' },
            { name: 'Belfast', imgId: 'icon-belfast' },
            { name: 'Dublin', imgId: 'icon-dublin' }
        ];
        for (const c of cities) {
            try {
                const resp = await fetch(`?handler=Weather&city=${encodeURIComponent(c.name)}`);
                if (!resp.ok) continue;
                const data = await resp.json();
                const icon = data?.icon;
                const desc = data?.description || '';
                if (!icon) continue;
                const img = document.getElementById(c.imgId);
                if (img) {
                    // Correct OpenWeather icon URL
                    img.src = `https://openweathermap.org/img/wn/${icon}@2x.png`;
                    img.alt = `${c.name} ${desc}`;
                }
            } catch (err) {
                if (DEBUG) console.error('Weather fetch error for', c.name, err);
            }
        }
    }

    setWeatherIconsForCities();
});

// Measure header height and expose as CSS variable so hero image can use it
function setHeaderHeightVar() {
    const header = document.querySelector('header');
    if (!header) return;
    const rect = header.getBoundingClientRect();
    const height = Math.ceil(rect.height);
    document.documentElement.style.setProperty('--site-header-height', height + 'px');
}

window.addEventListener('load', setHeaderHeightVar);
window.addEventListener('resize', setHeaderHeightVar);
document.addEventListener('DOMContentLoaded', setHeaderHeightVar);

// Gathers current weather for five selected cities.
// These are static and never change, but may change in a future version.
// Or it may be made editable in a future version.
async function setWeatherIconsForCities() {
    const cities = [
        { name: 'New York', imgId: 'icon-newyork' },
        { name: 'London', imgId: 'icon-london' },
        { name: 'Tokyo', imgId: 'icon-japan' },
        { name: 'Belfast', imgId: 'icon-belfast' },
        { name: 'Dublin', imgId: 'icon-dublin' }
    ];

    for (const c of cities) {
        try {
            // Use a relative handler URL so this works whether Index page is routed at '/' or '/Index'.
            const resp = await fetch(`?handler=Weather&city=${encodeURIComponent(c.name)}`);
            if (!resp.ok) {
                console.warn('Server weather fetch error', c.name, resp.status);
                continue;
            }
            const data = await resp.json();
            console.debug('Weather handler response for', c.name, data);
            const icon = data?.icon;
            const desc = data?.description || '';
            if (!icon) continue;
            const img = document.getElementById(c.imgId);
            if (img) {
                // Use standard OpenWeather icon path from OpenWeather API
                const url = `https://openweathermap.org/img/wn/${icon}@2x.png`;
                if (DEBUG) console.debug('setting icon src for', c.name, url);
                img.src = url;
                img.alt = `${c.name} ${desc}`;
            }
        } catch (err) {
            console.error('Error fetching weather for', c.name, err);
        }
    }
}

document.addEventListener('DOMContentLoaded', function () {
    setWeatherIconsForCities();
});
