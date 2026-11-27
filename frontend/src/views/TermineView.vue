
<script setup lang="ts">
import { computed } from 'vue'
import { events } from '../data/events'

/* Convert a date in DD.MM.YYYY format into a Date object */
function datumUmwandeln(datum: string): Date {
  const [tag, monat, jahr] = datum.split('.').map(Number)

  if (
    tag === undefined ||
    monat === undefined ||
    jahr === undefined ||
    !Number.isFinite(tag) ||
    !Number.isFinite(monat) ||
    !Number.isFinite(jahr)
  ) {
    return new Date(NaN)
  }

  return new Date(jahr, monat - 1, tag)
}

/* Create today's date without a time */
function heutigesDatumErstellen(): Date {
  const heute = new Date()

  return new Date(
    heute.getFullYear(),
    heute.getMonth(),
    heute.getDate()
  )
}

/* Filter past events and sort the remaining events chronologically */
const kommendeTermine = computed(() => {
  const heute = heutigesDatumErstellen()

  return events
    .filter((event) => {
      const veranstaltungsDatum = datumUmwandeln(event.startDate)

      return veranstaltungsDatum >= heute
    })
    .sort((a, b) => {
      return (
        datumUmwandeln(a.startDate).getTime() -
        datumUmwandeln(b.startDate).getTime()
      )
    })
})
</script>

<template>
  <main class="termine-seite">
    <header class="seitenkopf">
      <p class="seiten-label">BBS I · SCHÜLERFIRMA</p>

      <h1>Unsere <span>Termine.</span></h1>

      <p class="beschreibung">
        Hier findet ihr uns mit unserem Stand – kommt vorbei!
        Wir freuen uns auf euren Besuch.
      </p>
    </header>

    <section class="termine-bereich" aria-labelledby="termine-titel">
      <div class="bereich-kopf">
        <div>
          <p class="bereich-label">Auf einen Blick</p>
          <h2 id="termine-titel">Wo ihr uns findet</h2>
        </div>

        <span class="termine-anzahl">
          {{ kommendeTermine.length }}
          {{ kommendeTermine.length === 1 ? 'Termin' : 'Termine' }}
        </span>
      </div>

      <div v-if="kommendeTermine.length > 0" class="termine-liste">
        <article
          v-for="(event, index) in kommendeTermine"
          :key="event.startDate + event.name"
          class="termin-eintrag"
        >
          <div class="termin-datum">
            <span class="datum-tag">
              {{ event.startDate.split('.')[0] }}
            </span>

            <span class="datum-monat">
              {{
                datumUmwandeln(event.startDate)
                  .toLocaleDateString('de-DE', { month: 'short' })
                  .replace('.', '')
                  .toUpperCase()
              }}
            </span>

            <span class="datum-jahr">
              {{ datumUmwandeln(event.startDate).getFullYear() }}
            </span>
          </div>

          <div class="termin-inhalt">
            <div class="termin-kopf">
              <div>
                <span class="termin-index">
                  TERMIN {{ String(index + 1).padStart(2, '0') }}
                </span>

                <h3>{{ event.name }}</h3>
              </div>

              <span class="termin-symbol" aria-hidden="true">?</span>
            </div>

            <div class="termin-meta">
              <span v-if="event.startTime">
                <span class="meta-symbol" aria-hidden="true">?</span>
                <span>
                  {{ event.startTime }}
                  <template v-if="event.endTime">
                    – {{ event.endTime }}
                  </template>
                  Uhr
                </span>
              </span>

              <span>
                <span class="meta-symbol" aria-hidden="true">?</span>
                <span>{{ event.location }}</span>
              </span>
            </div>

            <p v-if="event.description" class="termin-beschreibung">
              {{ event.description }}
            </p>
          </div>
        </article>
      </div>

      <div v-else class="keine-termine">
        <span class="leer-symbol" aria-hidden="true">?</span>
        <h3>Im Moment ist es ruhig.</h3>
        <p>
          Aktuell sind keine Termine geplant. Schaut bald wieder vorbei!
        </p>
      </div>
    </section>

    <aside class="archiv-hinweis">
      <div>
        <p class="archiv-label">Schon neugierig?</p>
        <p>
          Ihr möchtet wissen, was wir bereits erlebt haben?
          <RouterLink to="/archiv">
            Entdeckt unser Veranstaltungsarchiv.
          </RouterLink>
        </p>
      </div>

      <span class="archiv-symbol" aria-hidden="true">?</span>
    </aside>
  </main>
</template>

<style scoped>
.termine-seite {
  --farbe-hintergrund: #f5f4ef;
  --farbe-blau: #4d648a;
  --farbe-dunkel: #1e222c;
  --farbe-salbei: #777659;
  --farbe-grau: #8b949b;
  --farbe-terracotta: #d9a477;
  --farbe-rand: #deddd5;

  min-height: 100vh;
  padding: 6rem 7%;
  background: var(--farbe-hintergrund);
  color: var(--farbe-dunkel);
}

.termine-seite,
.termine-seite * {
  box-sizing: border-box;
}

.seitenkopf,
.termine-bereich,
.archiv-hinweis {
  width: 100%;
  max-width: 1500px;
  margin-right: auto;
  margin-left: auto;
}

/* Page heading */
.seitenkopf {
  margin-bottom: 5rem;
}

.seiten-label {
  margin: 0 0 1.2rem;
  color: var(--farbe-salbei);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.16em;
}

.seitenkopf h1 {
  margin: 0;
  color: var(--farbe-dunkel);
  font-size: clamp(3.2rem, 5vw, 5rem);
  font-weight: 800;
  letter-spacing: -0.075em;
  line-height: 0.98;
}

.seitenkopf h1 span {
  color: var(--farbe-blau);
}

.beschreibung {
  max-width: 490px;
  margin: 1.3rem 0 0;
  color: #68717c;
  font-size: 0.9rem;
  line-height: 1.8;
}

/* Section heading */
.bereich-kopf {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 2rem;
  margin-bottom: 2rem;
  padding-bottom: 1.5rem;
  border-bottom: 1px solid var(--farbe-rand);
}

.bereich-label {
  margin: 0 0 0.6rem;
  color: var(--farbe-salbei);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.bereich-kopf h2 {
  margin: 0;
  color: var(--farbe-dunkel);
  font-size: clamp(2rem, 3.5vw, 3rem);
  font-weight: 800;
  letter-spacing: -0.065em;
  line-height: 1.05;
}

.termine-anzahl {
  flex-shrink: 0;
  padding: 0.6rem 1rem;
  border: 1px solid var(--farbe-rand);
  border-radius: 999px;
  background: #ffffff;
  color: var(--farbe-blau);
  font-size: 0.75rem;
  font-weight: 700;
}

/* Event list */
.termine-liste {
  border-top: 2px solid var(--farbe-dunkel);
}

.termin-eintrag {
  display: grid;
  grid-template-columns: 145px minmax(0, 1fr);
  gap: 2.5rem;
  padding: 2.3rem 1.5rem;
  border-bottom: 1px solid var(--farbe-rand);
  transition: background 0.25s ease, padding 0.25s ease;
}

.termin-eintrag:hover {
  padding-right: 2rem;
  padding-left: 2rem;
  background: rgb(255 255 255 / 65%);
}

/* Date */
.termin-datum {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  align-self: flex-start;
  padding-left: 1rem;
  border-left: 4px solid var(--farbe-terracotta);
}

.datum-tag {
  color: var(--farbe-blau);
  font-size: 3.8rem;
  font-weight: 800;
  letter-spacing: -0.09em;
  line-height: 0.95;
}

.datum-monat {
  margin-top: 0.4rem;
  color: var(--farbe-salbei);
  font-size: 0.85rem;
  font-weight: 800;
  letter-spacing: 0.12em;
}

.datum-jahr {
  margin-top: 0.25rem;
  color: var(--farbe-grau);
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.08em;
}

/* Event information */
.termin-inhalt {
  min-width: 0;
}

.termin-kopf {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1.5rem;
}

.termin-index {
  display: block;
  margin-bottom: 0.55rem;
  color: var(--farbe-grau);
  font-size: 0.62rem;
  font-weight: 700;
  letter-spacing: 0.14em;
}

.termin-kopf h3 {
  margin: 0;
  color: var(--farbe-dunkel);
  font-size: clamp(1.35rem, 2.2vw, 1.8rem);
  font-weight: 800;
  letter-spacing: -0.045em;
  line-height: 1.2;
  overflow-wrap: anywhere;
  transition: color 0.2s ease;
}

.termin-eintrag:hover .termin-kopf h3 {
  color: var(--farbe-blau);
}

.termin-symbol {
  flex-shrink: 0;
  color: var(--farbe-terracotta);
  font-size: 1.5rem;
  transition: transform 0.2s ease;
}

.termin-eintrag:hover .termin-symbol {
  transform: translate(4px, -4px);
}

/* Event details */
.termin-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 0.7rem 1.8rem;
  margin-top: 1rem;
  color: #68717c;
  font-size: 0.8rem;
  line-height: 1.5;
}

.termin-meta > span {
  display: flex;
  align-items: flex-start;
  gap: 0.55rem;
}

.meta-symbol {
  flex-shrink: 0;
  color: var(--farbe-salbei);
  font-size: 1rem;
}

.termin-beschreibung {
  max-width: 700px;
  margin: 1rem 0 0;
  color: #7b8188;
  font-size: 0.85rem;
  line-height: 1.75;
}

/* No events */
.keine-termine {
  padding: 4rem 2rem;
  border-bottom: 1px solid var(--farbe-rand);
  text-align: center;
}

.leer-symbol {
  display: block;
  margin-bottom: 1rem;
  color: var(--farbe-terracotta);
  font-size: 2rem;
}

.keine-termine h3 {
  margin: 0 0 0.5rem;
  color: var(--farbe-dunkel);
  font-size: 1.4rem;
  font-weight: 750;
}

.keine-termine p {
  margin: 0;
  color: var(--farbe-grau);
  font-size: 0.85rem;
  line-height: 1.7;
}

/* Archive hint */
.archiv-hinweis {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.5rem;
  margin-top: 4rem;
  padding: 1.8rem 2rem;
  border: 1px solid var(--farbe-rand);
  border-radius: 16px 16px 16px 4px;
  background: #e8e5dc;
}

.archiv-label {
  margin: 0 0 0.35rem;
  color: var(--farbe-salbei);
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.archiv-hinweis p:last-child {
  margin: 0;
  color: var(--farbe-dunkel);
  font-size: 0.85rem;
  line-height: 1.7;
}

.archiv-hinweis a {
  color: var(--farbe-blau);
  font-weight: 700;
  text-decoration-color: var(--farbe-terracotta);
  text-underline-offset: 4px;
}

.archiv-hinweis a:hover {
  color: var(--farbe-salbei);
}

.archiv-symbol {
  display: flex;
  flex-shrink: 0;
  width: 42px;
  height: 42px;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: var(--farbe-dunkel);
  color: #ffffff;
  font-size: 1.2rem;
}

/* Responsive layout */
@media (max-width: 700px) {
  .termine-seite {
    padding: 4rem 1.25rem;
  }

  .seitenkopf {
    margin-bottom: 3.5rem;
  }

  .seitenkopf h1 {
    font-size: clamp(3rem, 12vw, 4.5rem);
  }

  .bereich-kopf {
    align-items: flex-start;
    flex-direction: column;
    gap: 1rem;
  }

  .termin-eintrag {
    grid-template-columns: 85px minmax(0, 1fr);
    gap: 1.2rem;
    padding: 1.8rem 0.5rem;
  }

  .termin-eintrag:hover {
    padding-right: 0.8rem;
    padding-left: 0.8rem;
  }

  .termin-datum {
    padding-left: 0.65rem;
    border-left-width: 3px;
  }

  .datum-tag {
    font-size: 2.8rem;
  }

  .datum-monat {
    font-size: 0.7rem;
  }

  .datum-jahr {
    font-size: 0.65rem;
  }

  .termin-kopf {
    gap: 0.5rem;
  }

  .termin-kopf h3 {
    font-size: 1.2rem;
  }

  .termin-meta {
    flex-direction: column;
    gap: 0.5rem;
  }

  .archiv-hinweis {
    padding: 1.2rem;
  }
}

@media (max-width: 380px) {
  .termin-eintrag {
    grid-template-columns: 70px minmax(0, 1fr);
    gap: 0.8rem;
  }

  .datum-tag {
    font-size: 2.4rem;
  }

  .termin-kopf h3 {
    font-size: 1.05rem;
  }

  .termin-symbol {
    font-size: 1.1rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  .termin-eintrag,
  .termin-kopf h3,
  .termin-symbol {
    transition: none;
  }
}
</style>
