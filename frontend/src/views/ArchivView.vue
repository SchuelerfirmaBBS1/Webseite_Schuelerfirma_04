
<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue'

interface ArchivBild {
  id: number
  titel: string
  datum: string
  jahr: number
  beschreibung: string
  bild: string
  alt: string
}

const archivBilder: ArchivBild[] = [
  {
    id: 1,
    titel: 'Unser Verkaufsstand',
    datum: '10. September 2026',
    jahr: 2026,
    beschreibung:
      'Ein Rückblick auf unseren Verkaufsstand in der Pausenhalle.',
    bild: 'https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=1400&q=85',
    alt: 'Produkte und Verkaufsfläche an einem Verkaufsstand',
  },
  {
    id: 2,
    titel: 'Ein Tag voller Begegnungen',
    datum: '15. Mai 2026',
    jahr: 2026,
    beschreibung:
      'Ein besonderer Tag mit vielen interessanten Begegnungen.',
    bild: 'https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=1400&q=85',
    alt: 'Veranstaltung mit gedeckten Tischen',
  },
  {
    id: 3,
    titel: 'Gemeinsam anpacken',
    datum: '22. April 2026',
    jahr: 2026,
    beschreibung:
      'Teamarbeit und gemeinsame Ideen gehören zu unserer Schülerfirma.',
    bild: 'https://images.unsplash.com/photo-1511795409834-ef04bbd61622?auto=format&fit=crop&w=1400&q=85',
    alt: 'Gemeinsam an einem Tisch sitzen',
  },
]

const suche = ref('')
const ausgewaehltesJahr = ref('Alle')
const ausgewaehltesBild = ref<ArchivBild | null>(null)

const jahre = [
  'Alle',
  ...new Set(
    archivBilder
      .map((bild) => bild.jahr)
      .sort((a, b) => b - a)
      .map(String),
  ),
]

const gefilterteBilder = computed(() => {
  const suchtext = suche.value.trim().toLocaleLowerCase('de-DE')

  return archivBilder.filter((bild) => {
    const passtZurSuche =
      bild.titel.toLocaleLowerCase('de-DE').includes(suchtext) ||
      bild.beschreibung.toLocaleLowerCase('de-DE').includes(suchtext) ||
      bild.datum.toLocaleLowerCase('de-DE').includes(suchtext)

    const passtZumJahr =
      ausgewaehltesJahr.value === 'Alle' ||
      String(bild.jahr) === ausgewaehltesJahr.value

    return passtZurSuche && passtZumJahr
  })
})

function lightboxSchliessen() {
  ausgewaehltesBild.value = null
}

function tasteBehandeln(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    lightboxSchliessen()
  }
}

watch(ausgewaehltesBild, (bild) => {
  document.body.style.overflow = bild ? 'hidden' : ''
})

onMounted(() => {
  window.addEventListener('keydown', tasteBehandeln)
})

onUnmounted(() => {
  window.removeEventListener('keydown', tasteBehandeln)
  document.body.style.overflow = ''
})
</script>

<template>
  <main class="archiv-seite">
    <header class="seitenkopf">
      <p class="seiten-label">BBS I · SCHÜLERFIRMA</p>

      <h1>Veranstaltungsarchiv</h1>

      <p class="beschreibung">
        Erinnerungen, Begegnungen und besondere Momente –
        hier findet ihr Einblicke in unsere vergangenen Veranstaltungen.
      </p>
    </header>

    <section class="archiv-bereich" aria-labelledby="archiv-titel">
      <div class="bereich-kopf">
        <div>
          <p class="bereich-label">Rückblicke & Erinnerungen</p>
          <h2 id="archiv-titel">Unsere Bildergalerie</h2>
        </div>

        <span class="bilder-anzahl">
          {{ gefilterteBilder.length }}
          {{ gefilterteBilder.length === 1 ? 'Bild' : 'Bilder' }}
        </span>
      </div>

      <div class="filterbereich">
        <label class="suche">
          <span class="suchsymbol" aria-hidden="true">?</span>

          <input
            v-model="suche"
            type="search"
            placeholder="Veranstaltung suchen ..."
            aria-label="Bilder nach Veranstaltung, Beschreibung oder Datum suchen"
          />
        </label>

        <label class="jahresfilter">
          <span>Jahr</span>

          <select v-model="ausgewaehltesJahr" aria-label="Nach Jahr filtern">
            <option v-for="jahr in jahre" :key="jahr" :value="jahr">
              {{ jahr }}
            </option>
          </select>
        </label>
      </div>

      <div v-if="gefilterteBilder.length > 0" class="bilderraster">
        <button
          v-for="bild in gefilterteBilder"
          :key="bild.id"
          class="galerie-karte"
          type="button"
          :aria-label="`${bild.titel}, ${bild.datum}. Bild vergrößern.`"
          @click="ausgewaehltesBild = bild"
        >
          <div class="bildbereich">
            <img :src="bild.bild" :alt="bild.alt" loading="lazy" />

            <span class="bild-oeffnen" aria-hidden="true">
              <span>?</span>
            </span>
          </div>

          <div class="bild-info">
            <p class="bild-datum">{{ bild.datum }}</p>
            <h3>{{ bild.titel }}</h3>
            <p class="bild-beschreibung">{{ bild.beschreibung }}</p>
          </div>
        </button>
      </div>

      <div v-else class="keine-ergebnisse">
        <span class="leer-symbol" aria-hidden="true">?</span>
        <h3>Keine Bilder gefunden.</h3>
        <p>
          Versucht einen anderen Suchbegriff oder wählt ein anderes Jahr.
        </p>
      </div>
    </section>

    <aside class="archiv-hinweis">
      <div class="archiv-symbol" aria-hidden="true">?</div>

      <p>
        Ihr möchtet uns bei einer Veranstaltung besuchen?
        <a href="/termine">Hier findet ihr unsere kommenden Termine.</a>
      </p>
    </aside>

    <Teleport to="body">
      <div
        v-if="ausgewaehltesBild"
        class="lightbox"
        role="dialog"
        aria-modal="true"
        :aria-label="`Bildansicht: ${ausgewaehltesBild.titel}`"
        @click.self="lightboxSchliessen"
      >
       <button
        class="lightbox-schliessen"
        type="button"
        aria-label="Bildansicht schließen"
        @click.stop="lightboxSchliessen"
       >
        <span aria-hidden="true">×</span>
       </button>

        <div class="lightbox-inhalt">
          <img
            class="grosses-bild"
            :src="ausgewaehltesBild.bild"
            :alt="ausgewaehltesBild.alt"
          />

          <div class="lightbox-info">
            <p class="bild-datum">{{ ausgewaehltesBild.datum }}</p>
            <h2>{{ ausgewaehltesBild.titel }}</h2>
            <p>{{ ausgewaehltesBild.beschreibung }}</p>
          </div>
        </div>
      </div>
    </Teleport>
  </main>
</template>

<style scoped>
.archiv-seite {
  --farbe-hintergrund: #141a26;
  --farbe-karte: #202b3d;
  --farbe-karte-hover: #25334a;
  --farbe-akzent: #8badc9;
  --farbe-akzent-dunkel: #405d85;
  --farbe-text: #f2f4f8;
  --farbe-text-hell: #c2ccdc;
  --farbe-text-dezent: #a4b2c7;
  --farbe-rand: #34445d;

  min-height: 100vh;
  padding: 64px 24px 72px;
  background-color: var(--farbe-hintergrund);
  color: var(--farbe-text);
}

.archiv-seite,
.archiv-seite * {
  box-sizing: border-box;
}

.seitenkopf,
.archiv-bereich,
.archiv-hinweis {
  width: 100%;
  max-width: 850px;
  margin-right: auto;
  margin-left: auto;
}

.seitenkopf {
  margin-bottom: 56px;
}

.seiten-label {
  margin: 0 0 16px;
  color: var(--farbe-akzent);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 2.5px;
  line-height: 1.6;
  text-transform: uppercase;
}

.seitenkopf h1,
.bereich-kopf h2,
.keine-ergebnisse h3,
.lightbox-info h2 {
  font-family: Georgia, 'Times New Roman', serif;
  font-weight: 400;
}

.seitenkopf h1 {
  margin: 0 0 18px;
  font-size: clamp(42px, 6vw, 58px);
  letter-spacing: -1.5px;
  line-height: 1.15;
}

.beschreibung {
  max-width: 580px;
  margin: 0;
  color: var(--farbe-text-hell);
  font-size: 17px;
  line-height: 1.8;
}

.bereich-kopf {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 24px;
}

.bereich-label {
  margin: 0 0 7px;
  color: var(--farbe-akzent);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 1.8px;
  text-transform: uppercase;
}

.bereich-kopf h2 {
  margin: 0;
  font-size: clamp(25px, 4vw, 32px);
  line-height: 1.3;
}

.bilder-anzahl {
  flex-shrink: 0;
  padding: 8px 13px;
  border: 1px solid var(--farbe-rand);
  border-radius: 999px;
  background-color: #1b2637;
  color: var(--farbe-text-hell);
  font-size: 12px;
  white-space: nowrap;
}

.filterbereich {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 24px;
}

.suche {
  display: flex;
  flex: 1;
  align-items: center;
  gap: 10px;
  min-width: 0;
  padding: 0 14px;
  border: 1px solid var(--farbe-rand);
  border-radius: 12px;
  background-color: #1b2637;
}

.suchsymbol {
  flex-shrink: 0;
  color: var(--farbe-akzent);
  font-size: 22px;
}

.suche input {
  width: 100%;
  min-width: 0;
  padding: 13px 0;
  border: 0;
  outline: none;
  background: transparent;
  color: var(--farbe-text);
  font: inherit;
  font-size: 14px;
}

.suche input::placeholder {
  color: var(--farbe-text-dezent);
}

.jahresfilter {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--farbe-text-dezent);
  font-size: 13px;
}

.jahresfilter select {
  padding: 12px 13px;
  border: 1px solid var(--farbe-rand);
  border-radius: 10px;
  background-color: #1b2637;
  color: var(--farbe-text);
  font: inherit;
}

.bilderraster {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 16px;
}

.galerie-karte {
  min-width: 0;
  padding: 0;
  overflow: hidden;
  border: 1px solid var(--farbe-rand);
  border-radius: 17px;
  background-color: var(--farbe-karte);
  color: var(--farbe-text);
  text-align: left;
  cursor: pointer;
  transition:
    background-color 180ms ease,
    border-color 180ms ease,
    transform 180ms ease;
}

.galerie-karte:hover {
  transform: translateY(-2px);
  border-color: var(--farbe-akzent-dunkel);
  background-color: var(--farbe-karte-hover);
}

.galerie-karte:focus-visible,
.lightbox button:focus-visible,
.suche input:focus-visible,
.jahresfilter select:focus-visible {
  outline: 3px solid var(--farbe-akzent);
  outline-offset: 4px;
}

.bildbereich {
  position: relative;
  height: 230px;
  overflow: hidden;
  background-color: var(--farbe-karte-hover);
}

.bildbereich img {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
  transition: transform 250ms ease;
}

.galerie-karte:hover .bildbereich img {
  transform: scale(1.035);
}

.bild-oeffnen {
  position: absolute;
  right: 12px;
  bottom: 12px;
  display: flex;
  width: 36px;
  height: 36px;
  align-items: center;
  justify-content: center;
  border: 1px solid #ffffff35;
  border-radius: 50%;
  background-color: #141a26dd;
  color: var(--farbe-akzent);
  font-size: 19px;
  opacity: 0;
  transition: opacity 180ms ease;
}

.galerie-karte:hover .bild-oeffnen,
.galerie-karte:focus-visible .bild-oeffnen {
  opacity: 1;
}

.bild-info {
  padding: 20px;
}

.bild-datum {
  margin: 0 0 10px;
  color: var(--farbe-akzent);
  font-size: 12px;
  font-weight: 600;
}

.bild-info h3 {
  margin: 0 0 10px;
  color: var(--farbe-text);
  font-size: 18px;
  font-weight: 600;
  line-height: 1.45;
  overflow-wrap: anywhere;
}

.bild-beschreibung {
  margin: 0;
  color: var(--farbe-text-hell);
  font-size: 14px;
  line-height: 1.75;
  overflow-wrap: anywhere;
}

.keine-ergebnisse {
  padding: 36px 24px;
  border: 1px solid var(--farbe-rand);
  border-radius: 17px;
  background-color: var(--farbe-karte);
  text-align: center;
}

.leer-symbol {
  color: var(--farbe-akzent);
  font-size: 28px;
}

.keine-ergebnisse h3 {
  margin: 12px 0 8px;
  font-size: 23px;
}

.keine-ergebnisse p {
  margin: 0;
  color: var(--farbe-text-hell);
  font-size: 14px;
  line-height: 1.7;
}

.archiv-hinweis {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: 32px;
  padding: 20px 22px;
  border: 1px solid var(--farbe-rand);
  border-radius: 15px;
  background-color: #192334;
}

.archiv-symbol {
  display: flex;
  width: 38px;
  height: 38px;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
  background-color: #2b3d56;
  color: var(--farbe-akzent);
  font-size: 20px;
}

.archiv-hinweis p {
  margin: 0;
  color: var(--farbe-text-hell);
  font-size: 14px;
  line-height: 1.8;
}

.archiv-hinweis a {
  color: var(--farbe-text);
  font-weight: 600;
  text-decoration-color: var(--farbe-akzent);
  text-underline-offset: 4px;
}

.archiv-hinweis a:hover {
  color: var(--farbe-akzent);
}

.archiv-seite a:focus-visible {
  outline: 3px solid var(--farbe-akzent);
  outline-offset: 4px;
  border-radius: 3px;
}

.lightbox {
  position: fixed;
  z-index: 1000;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 56px 24px 24px;
  overflow-y: auto;
  background-color: rgb(8 12 20 / 94%);
  backdrop-filter: blur(5px);
}

.lightbox-inhalt {
  width: min(100%, 1000px);
  margin: auto;
  overflow: hidden;
  border: 1px solid var(--farbe-rand);
  border-radius: 16px;
  background-color: var(--farbe-karte);
  box-shadow: 0 24px 80px rgb(0 0 0 / 35%);
}

.grosses-bild {
  display: block;
  width: 100%;
  max-height: 68vh;
  background-color: var(--farbe-hintergrund);
  object-fit: contain;
}

.lightbox-info {
  padding: 22px 26px 26px;
}

.lightbox-info h2 {
  margin: 0 0 10px;
  font-size: clamp(25px, 4vw, 34px);
  line-height: 1.3;
}

.lightbox-info > p:last-child {
  margin: 0;
  color: var(--farbe-text-hell);
  font-size: 14px;
  line-height: 1.8;
}


.lightbox-schliessen {
  position: absolute;
  z-index: 1002;
  top: 16px;
  right: 16px;

  display: flex;
  align-items: center;
  justify-content: center;

  width: 42px;
  height: 42px;
  padding: 0;

  border: 1px solid #8badc9;
  border-radius: 50%;
  background-color: #202b3d;
  color: #f2f4f8;

  font-size: 28px;
  line-height: 1;
  cursor: pointer;
}

.lightbox-schliessen:hover {
  background-color: #405d85;
}

.lightbox-schliessen span {
  display: block;
  transform: translateY(-1px);
}

@media (max-width: 600px) {
  .archiv-seite {
    padding: 40px 16px 48px;
  }

  .seitenkopf {
    margin-bottom: 40px;
  }

  .seitenkopf h1 {
    font-size: 42px;
  }

  .beschreibung {
    font-size: 16px;
  }

  .bereich-kopf {
    align-items: flex-start;
    gap: 12px;
  }

  .bereich-kopf h2 {
    font-size: 26px;
  }

  .bilder-anzahl {
    padding: 7px 10px;
    font-size: 11px;
  }

  .filterbereich {
    align-items: stretch;
    flex-direction: column;
  }

  .jahresfilter {
    justify-content: space-between;
  }

  .jahresfilter select {
    flex: 1;
  }

  .bilderraster {
    grid-template-columns: 1fr;
  }

  .bildbereich {
    height: 230px;
  }

  .bild-oeffnen {
    opacity: 1;
  }

  .archiv-hinweis {
    gap: 12px;
    padding: 16px;
  }

  .lightbox {
    padding: 64px 12px 16px;
  }

  .lightbox-info {
    padding: 18px;
  }

  .grosses-bild {
    max-height: 58vh;
  }

  .lightbox-schliessen {
    top: 12px;
    right: 12px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .galerie-karte,
  .bildbereich img,
  .bild-oeffnen {
    transition: none;
  }

  .galerie-karte:hover,
  .galerie-karte:hover .bildbereich img {
    transform: none;
  }
}
</style>
