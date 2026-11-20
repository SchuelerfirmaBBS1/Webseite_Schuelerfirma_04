
<script setup lang="ts">
import { computed } from 'vue'
import { events } from '../data/events'

/* Convert a date in DD.MM.YYYY format into a Date object */
function datumUmwandeln(datum: string): Date {
  const [tag, monat, jahr] = datum.split('.').map(Number)

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

      <h1>Termine<span>.</span></h1>

      <p class="beschreibung">
        Hier findet ihr uns mit unserem Stand – kommt vorbei!
        Wir freuen uns auf euren Besuch.
      </p>
    </header>

    <section class="termine-bereich" aria-labelledby="termine-titel">
      <div class="bereich-kopf">
        <div>
          <p class="bereich-label">Auf einen Blick</p>
          <h2 id="termine-titel">Kommende Termine</h2>
        </div>

        <span class="termine-anzahl">
          {{ kommendeTermine.length }}
          {{ kommendeTermine.length === 1 ? 'Termin' : 'Termine' }}
        </span>
      </div>

      <div v-if="kommendeTermine.length > 0" class="termine-liste">
        <article
          v-for="event in kommendeTermine"
          :key="event.startDate + event.name"
          class="termin-karte"
        >
          <div class="datum" aria-hidden="true">
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
          </div>

          <div class="termin-info">
            <h3>{{ event.name }}</h3>

            <p class="termin-meta">
              <span v-if="event.startTime" class="meta-element">
                <span class="meta-symbol" aria-hidden="true">◷</span>
                <span>
                  {{ event.startTime }}
                  <template v-if="event.endTime">
                    – {{ event.endTime }}
                  </template>
                  Uhr
                </span>
              </span>

              <span class="meta-element">
                <span class="meta-symbol" aria-hidden="true">⌖</span>
                <span>{{ event.location }}</span>
              </span>
            </p>

            <p v-if="event.description" class="termin-beschreibung">
              {{ event.description }}
            </p>
          </div>

          <span class="karten-akzent" aria-hidden="true">↗</span>
        </article>
      </div>

      <div v-else class="keine-termine">
        <span class="leer-symbol" aria-hidden="true">✳</span>
        <h3>Im Moment ist es ruhig.</h3>
        <p>
          Aktuell sind keine Termine geplant. Schaut bald wieder vorbei!
        </p>
      </div>
    </section>

    <aside class="archiv-hinweis">
      <div class="archiv-symbol" aria-hidden="true">↗</div>

      <p>
        Ihr möchtet wissen, was wir bereits erlebt haben?
        <RouterLink to="/archiv">
          Entdeckt unser Veranstaltungsarchiv.
        </RouterLink>
      </p>
    </aside>
  </main>
</template>

<style scoped>
.termine-seite {
  --farbe-hintergrund: #f5f1e8;
  --farbe-karte: #ffffff;
  --farbe-karte-hover: #fafbf7;
  --farbe-blau: #202d43;
  --farbe-salbei: #a4aa91;
  --farbe-salbei-hell: #e9ece3;
  --farbe-text: #343b40;
  --farbe-text-hell: #747a78;
  --farbe-rand: #e4e5dc;

  min-height: 100vh;
  padding: 64px 24px 76px;
  background-color: var(--farbe-hintergrund);
  color: var(--farbe-text);
}

.termine-seite,
.termine-seite * {
  box-sizing: border-box;
}

.seitenkopf,
.termine-bereich,
.archiv-hinweis {
  width: 100%;
  max-width: 850px;
  margin-right: auto;
  margin-left: auto;
}

.seitenkopf {
  margin-bottom: 48px;
}

.seiten-label {
  margin: 0 0 15px;
  color: #81896f;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 2.5px;
  line-height: 1.6;
  text-transform: uppercase;
}

.seitenkopf h1 {
  margin: 0 0 15px;
  color: var(--farbe-blau);
  font-family: Georgia, 'Times New Roman', serif;
  font-size: clamp(44px, 6vw, 60px);
  font-weight: 400;
  letter-spacing: -1.5px;
  line-height: 1.15;
}

.seitenkopf h1 span {
  color: var(--farbe-salbei);
}

.beschreibung {
  max-width: 580px;
  margin: 0;
  color: var(--farbe-text-hell);
  font-size: 16px;
  line-height: 1.8;
}

.bereich-kopf {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 20px;
  margin-bottom: 22px;
}

.bereich-label {
  margin: 0 0 7px;
  color: #81896f;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 1.8px;
  text-transform: uppercase;
}

.bereich-kopf h2 {
  margin: 0;
  color: var(--farbe-blau);
  font-family: Georgia, 'Times New Roman', serif;
  font-size: clamp(26px, 4vw, 34px);
  font-weight: 400;
  line-height: 1.3;
}

.termine-anzahl {
  flex-shrink: 0;
  padding: 8px 14px;
  border: 1px solid #dce1d3;
  border-radius: 999px;
  background-color: var(--farbe-salbei-hell);
  color: #59634f;
  font-size: 12px;
  font-weight: 600;
  white-space: nowrap;
}

.termine-liste {
  display: grid;
  gap: 13px;
}

.termin-karte {
  position: relative;
  display: flex;
  align-items: center;
  gap: 22px;
  min-width: 0;
  padding: 20px 22px;
  overflow: hidden;
  border: 1px solid var(--farbe-rand);
  border-radius: 15px;
  background-color: var(--farbe-karte);
  box-shadow: 0 3px 12px rgb(32 45 67 / 3%);
  transition:
    background-color 180ms ease,
    border-color 180ms ease,
    box-shadow 180ms ease,
    transform 180ms ease;
}

.termin-karte:hover {
  transform: translateY(-2px);
  border-color: #cbd2c0;
  background-color: var(--farbe-karte-hover);
  box-shadow: 0 8px 22px rgb(32 45 67 / 7%);
}

.datum {
  display: flex;
  flex-shrink: 0;
  width: 76px;
  min-height: 82px;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border-radius: 11px;
  background-color: var(--farbe-blau);
  color: #ffffff;
}

.datum-tag {
  font-family: Georgia, 'Times New Roman', serif;
  font-size: 30px;
  line-height: 1.2;
}

.datum-monat {
  margin-top: 5px;
  color: #d7dfd2;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 1.3px;
}

.termin-info {
  flex: 1;
  min-width: 0;
}

.termin-info h3 {
  margin: 0 0 9px;
  color: var(--farbe-blau);
  font-size: 18px;
  font-weight: 650;
  line-height: 1.45;
  overflow-wrap: anywhere;
}

.termin-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 7px 18px;
  margin: 0;
  color: var(--farbe-text-hell);
  font-size: 13px;
  line-height: 1.7;
}

.meta-element {
  display: inline-flex;
  align-items: flex-start;
  gap: 7px;
  min-width: 0;
  overflow-wrap: anywhere;
}

.meta-symbol {
  flex-shrink: 0;
  color: #81896f;
  font-size: 17px;
  line-height: 1.35;
}

.termin-info .termin-beschreibung {
  margin: 9px 0 0;
  color: var(--farbe-text-hell);
  font-size: 14px;
  line-height: 1.7;
  overflow-wrap: anywhere;
}

.karten-akzent {
  display: flex;
  flex-shrink: 0;
  width: 35px;
  height: 35px;
  align-items: center;
  justify-content: center;
  border: 1px solid var(--farbe-rand);
  border-radius: 50%;
  color: #81896f;
  font-size: 17px;
  transition:
    background-color 180ms ease,
    border-color 180ms ease,
    color 180ms ease;
}

.termin-karte:hover .karten-akzent {
  border-color: var(--farbe-salbei);
  background-color: var(--farbe-salbei-hell);
  color: var(--farbe-blau);
}

.keine-termine {
  padding: 38px 24px;
  border: 1px solid var(--farbe-rand);
  border-radius: 15px;
  background-color: var(--farbe-karte);
  text-align: center;
}

.leer-symbol {
  color: #81896f;
  font-size: 28px;
}

.keine-termine h3 {
  margin: 12px 0 8px;
  color: var(--farbe-blau);
  font-family: Georgia, 'Times New Roman', serif;
  font-size: 24px;
  font-weight: 400;
}

.keine-termine p {
  margin: 0;
  color: var(--farbe-text-hell);
  font-size: 14px;
  line-height: 1.7;
}

.archiv-hinweis {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: 28px;
  padding: 19px 21px;
  border: 1px solid #e0e3d8;
  border-radius: 14px;
  background-color: #e9ece3;
}

.archiv-symbol {
  display: flex;
  flex-shrink: 0;
  width: 38px;
  height: 38px;
  align-items: center;
  justify-content: center;
  border-radius: 10px;
  background-color: var(--farbe-blau);
  color: #ffffff;
  font-size: 19px;
}

.archiv-hinweis p {
  margin: 0;
  color: #59615a;
  font-size: 14px;
  line-height: 1.8;
}

.archiv-hinweis a {
  color: var(--farbe-blau);
  font-weight: 700;
  text-decoration-color: #81896f;
  text-underline-offset: 4px;
}

.archiv-hinweis a:hover {
  color: #687451;
}

.termine-seite a:focus-visible {
  outline: 3px solid #81896f;
  outline-offset: 4px;
  border-radius: 3px;
}

@media (max-width: 600px) {
  .termine-seite {
    padding: 42px 16px 52px;
  }

  .seitenkopf {
    margin-bottom: 38px;
  }

  .seitenkopf h1 {
    font-size: 44px;
  }

  .beschreibung {
    font-size: 15px;
  }

  .bereich-kopf {
    align-items: flex-start;
    gap: 12px;
  }

  .bereich-kopf h2 {
    font-size: 27px;
  }

  .termine-anzahl {
    padding: 7px 10px;
    font-size: 11px;
  }

  .termin-karte {
    gap: 14px;
    padding: 14px;
    border-radius: 13px;
  }

  .datum {
    width: 62px;
    min-height: 72px;
    border-radius: 9px;
  }

  .datum-tag {
    font-size: 26px;
  }

  .datum-monat {
    font-size: 10px;
  }

  .termin-info h3 {
    font-size: 16px;
  }

  .termin-meta {
    gap: 5px 12px;
    font-size: 12px;
  }

  .termin-info .termin-beschreibung {
    font-size: 13px;
  }

  .karten-akzent {
    display: none;
  }

  .archiv-hinweis {
    gap: 12px;
    padding: 15px;
  }
}

@media (max-width: 380px) {
  .bereich-kopf {
    flex-direction: column;
    align-items: flex-start;
  }

  .termin-karte {
    align-items: flex-start;
    gap: 12px;
    padding: 12px;
  }

  .datum {
    width: 54px;
    min-height: 66px;
  }

  .datum-tag {
    font-size: 23px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .termin-karte,
  .karten-akzent {
    transition: none;
  }

  .termin-karte:hover {
    transform: none;
  }
}
</style>
