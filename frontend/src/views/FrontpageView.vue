
<script setup lang="ts">
import { computed, ref } from 'vue'
import { events } from '../data/events'

/* Convert a date in DD.MM.YYYY format into a Date object */
function parseEventDate(dateString: string): Date {
  const [day, month, year] = dateString.split('.').map(Number)
  return new Date(year, month - 1, day)
}

/* Get the next three upcoming events */
const upcomingEvents = computed(() => {
  const today = new Date()
  today.setHours(0, 0, 0, 0)

  return events
    .map((event) => ({
      ...event,
      parsedDate: parseEventDate(event.startDate),
    }))
    .filter((event) => event.parsedDate >= today)
    .sort((a, b) => a.parsedDate.getTime() - b.parsedDate.getTime())
    .slice(0, 3)
})

/* Format the date for the event cards */
function getDay(dateString: string): string {
  return dateString.split('.')[0]
}

function getMonth(dateString: string): string {
  const month = Number(dateString.split('.')[1])

  return [
    'JAN',
    'FEB',
    'MÄR',
    'APR',
    'MAI',
    'JUN',
    'JUL',
    'AUG',
    'SEP',
    'OKT',
    'NOV',
    'DEZ',
  ][month - 1] ?? ''
}

/* Contact form */
const contactForm = ref({
  name: '',
  email: '',
  subject: '',
  message: '',
})

const contactFeedback = ref('')

function handleContactSubmit() {
  contactFeedback.value =
    'Das Formular ist noch nicht verbunden. Deine Nachricht wurde nicht versendet.'
}
</script>

<template>
  <main class="home">
    <!-- Hero section -->
    <section class="hero">
      <div class="hero-content">
        <div class="hero-copy">
          <p class="eyebrow">SCHÜLERUNTERNEHMEN · BBS I LÜNEBURG</p>

          <h1>
            Kreative Ideen.
            <span>Nachhaltige Produkte.</span>
            Echter Genuss.
          </h1>

          <p class="hero-description">
            Wir verbinden moderne Technik, kreatives Handwerk
            und nachhaltiges Wirtschaften. Von der Herstellung
            bis zum Verkauf – bei uns steckt die ganze Klasse dahinter.
          </p>

          <a href="#produkte" class="primary-link">
            Unsere Produktwelt entdecken
            <span aria-hidden="true">?</span>
          </a>
        </div>

        <div class="hero-visual">
          <img
            src="/images/hero-print-bite.png"
            alt="Einblicke in die Produkte von PRINT&BITE"
            class="hero-image"
          />
        </div>
      </div>
    </section>

    <!-- Product section -->
    <section id="produkte" class="products-section">
      <div class="section-heading">
        <div>
          <p class="eyebrow">UNSERE PRODUKTWELT</p>
          <h2>
            Kreativität trifft
            <span>auf Handwerk.</span>
          </h2>
        </div>

        <p class="section-intro">
          Bei PRINT&BITE entstehen Produkte mit Persönlichkeit.
          Wir verbinden moderne 3D-Drucktechnik mit traditionellem
          Handwerk und setzen kreative Ideen in die Realität um.
        </p>
      </div>

      <div class="product-grid">
        <!-- PRINT -->
        <article class="product-card">
          <div class="product-visual print-visual">
            <span class="visual-number">01</span>
            <span class="visual-category">KREATIVITÄT IN 3D</span>

            <div class="visual-placeholder">
              <span>PRINT</span>
              <small>3D-gedruckte Produkte</small>
            </div>
          </div>

          <div class="product-info">
            <div>
              <p class="eyebrow">PRINT</p>
              <h3>Ideen werden greifbar.</h3>
              <p>
                Kreative Produkte aus dem 3D-Drucker,
                hergestellt aus recyceltem Kunststoff.
              </p>
            </div>

            <span class="product-arrow" aria-hidden="true">?</span>
          </div>
        </article>

        <!-- HANDWERK -->
        <article class="product-card">
          <div class="product-visual wood-visual">
            <span class="visual-number">02</span>
            <span class="visual-category">HANDWERK MIT CHARAKTER</span>

            <div class="visual-placeholder">
              <span>HANDWERK</span>
              <small>Produkte aus Holz</small>
            </div>
          </div>

          <div class="product-info">
            <div>
              <p class="eyebrow">HANDWERK</p>
              <h3>Natürlich besonders.</h3>
              <p>
                Hochwertige Holzprodukte in Zusammenarbeit
                mit der BBS II.
              </p>
            </div>

            <span class="product-arrow" aria-hidden="true">?</span>
          </div>
        </article>
      </div>
    </section>

    <!-- Sustainability section -->
    <section class="sustainability-section">
      <div class="sustainability-content">
        <div class="sustainability-copy">
          <p class="eyebrow">BEWUSST GESTALTEN · NEU DENKEN</p>

          <h2>
            Aus Alt wird
            <span>etwas Neues.</span>
          </h2>

          <p class="sustainability-description">
            Nachhaltigkeit beginnt bei den Materialien, die wir
            verwenden. Beim 3D-Druck gibt es Möglichkeiten,
            Kunststoff wiederzuverwenden und daraus neues
            Filament für weitere Produkte herzustellen.
          </p>

          <p class="sustainability-description">
            Die Recyclingfabrik aus Braunschweig zeigt, wie aus
            Kunststoffresten wieder ein wertvoller Rohstoff
            entstehen kann. So wird aus einem vermeintlichen
            Abfallprodukt eine neue Möglichkeit.
          </p>

          <a
            href="https://www.recyclingfabrik.com/"
            target="_blank"
            rel="noopener noreferrer"
            class="sustainability-link"
          >
            Die Recyclingfabrik kennenlernen
            <span aria-hidden="true">?</span>
          </a>
        </div>

        <div class="sustainability-visual">
          <div class="sustainability-image-placeholder">
            <span class="recycling-symbol" aria-hidden="true">?</span>
            <p>RECYCLING</p>
            <span>Aus Kunststoff wird neues Filament.</span>
          </div>

          <div class="sustainability-caption">
            <span>Ressourcen weiterdenken.</span>
            <span>PRINT&BITE</span>
          </div>
        </div>
      </div>

      <div class="quiz-teaser">
        <div>
          <p class="eyebrow">WISSEN, DAS WEITERHILFT</p>
          <h3>Wie gut kennst du dich mit Plastik und Wasser aus?</h3>
          <p>
            Entdecke spannende Fakten rund um Kunststoff,
            Recycling und einen bewussten Umgang mit Ressourcen.
          </p>
        </div>

        <div class="quiz-decoration" aria-hidden="true">
          <span>?</span>
        </div>
      </div>
    </section>

    <!-- Upcoming events section -->
    <section class="events-preview-section">
      <div class="events-preview-heading">
        <div>
          <p class="eyebrow">IMMER AUF DEM LAUFENDEN</p>
          <h2>
            Was bei uns
            <span>ansteht.</span>
          </h2>
        </div>

        <RouterLink to="/termine" class="all-events-link">
          Alle Termine ansehen
          <span aria-hidden="true">?</span>
        </RouterLink>
      </div>

      <div v-if="upcomingEvents.length > 0" class="events-preview-grid">
        <article
          v-for="event in upcomingEvents"
          :key="event.name + event.startDate"
          class="event-preview-card"
        >
          <div class="event-date">
            <span class="event-day">{{ getDay(event.startDate) }}</span>
            <span class="event-month">{{ getMonth(event.startDate) }}</span>
          </div>

          <div class="event-details">
            <h3>{{ event.name }}</h3>

            <div class="event-meta">
              <span v-if="event.startTime || event.endTime">
                <span aria-hidden="true">?</span>
                {{ event.startTime }}
                <template v-if="event.endTime">
                  – {{ event.endTime }}
                </template>
              </span>

              <span>
                <span aria-hidden="true">?</span>
                {{ event.location }}
              </span>
            </div>

            <p v-if="event.description" class="event-description">
              {{ event.description }}
            </p>
          </div>

          <span class="event-card-arrow" aria-hidden="true">?</span>
        </article>
      </div>

      <div v-else class="no-events">
        <p>Aktuell sind keine kommenden Termine geplant.</p>
        <span>Schau gerne später wieder vorbei!</span>
      </div>

      <div class="events-preview-footer">
        <span>Neue Termine und Veranstaltungen findest du hier.</span>

        <RouterLink to="/termine">
          Zur Terminübersicht
          <span aria-hidden="true">?</span>
        </RouterLink>
      </div>
    </section>

    <!-- About us section -->
    <section id="ueber-uns" class="about-section">
      <div class="about-content">
        <div class="about-visual">
          <div class="about-image-placeholder">
            <span class="about-image-label">HA26B · BBS I LÜNEBURG</span>

            <div class="about-image-symbol" aria-hidden="true">
              <span></span>
              <span></span>
              <span></span>
            </div>

            <div class="about-image-text">
              <span>HIER ENTSTEHT</span>
              <strong>PRINT&BITE</strong>
            </div>

            <span class="about-image-caption">
              Unser Team · Unsere Ideen · Unser Unternehmen
            </span>
          </div>
        </div>

        <div class="about-copy">
          <p class="eyebrow">DAS TEAM HINTER PRINT&BITE</p>

          <h2>
            Wir sind
            <span>PRINT&BITE.</span>
          </h2>

          <p class="about-intro">
            Hinter PRINT&BITE steckt die Klasse HA26B der
            BBS I Lüneburg. Gemeinsam haben wir unser eigenes
            Schülerunternehmen auf die Beine gestellt.
          </p>

          <p class="about-description">
            Von der Entwicklung und Herstellung unserer Produkte
            über den Verkauf bis hin zu Marketing und Organisation:
            Jede und jeder bringt eigene Ideen und Fähigkeiten ein.
            Dabei sammeln wir praktische Erfahrungen und lernen,
            was es bedeutet, ein Unternehmen gemeinsam zu führen.
          </p>

          <p class="about-description">
            Was uns verbindet, ist die Freude daran, etwas Eigenes
            zu schaffen und aus kreativen Ideen echte Produkte
            werden zu lassen.
          </p>

          <div class="about-values">
            <span>Teamwork</span>
            <span>Kreativität</span>
            <span>Verantwortung</span>
          </div>
        </div>
      </div>
    </section>

    <!-- Contact section -->
    <section id="kontakt" class="contact-section">
      <div class="contact-content">
        <div class="contact-copy">
          <p class="eyebrow">WIR SIND FÜR DICH DA</p>

          <h2>
            Wir freuen uns
            <span>auf deine Nachricht.</span>
          </h2>

          <p class="contact-description">
            Du hast Fragen zu unseren Produkten, möchtest mehr
            über unser Schülerunternehmen erfahren oder mit uns
            in Kontakt treten? Dann schreib uns gerne!
          </p>

          <div class="contact-note">
            <span class="contact-note-symbol" aria-hidden="true">?</span>
            <p>
              Wir freuen uns über dein Interesse an PRINT&BITE
              und melden uns gerne bei dir.
            </p>
          </div>
        </div>

        <form class="contact-form" @submit.prevent="handleContactSubmit">
          <div class="form-row">
            <div class="form-group">
              <label for="contact-name">Dein Name</label>
              <input
                id="contact-name"
                v-model="contactForm.name"
                type="text"
                name="name"
                placeholder="Vor- und Nachname"
                autocomplete="name"
                required
              />
            </div>

            <div class="form-group">
              <label for="contact-email">Deine E-Mail-Adresse</label>
              <input
                id="contact-email"
                v-model="contactForm.email"
                type="email"
                name="email"
                placeholder="name@beispiel.de"
                autocomplete="email"
                required
              />
            </div>
          </div>

          <div class="form-group">
            <label for="contact-subject">Betreff</label>
            <input
              id="contact-subject"
              v-model="contactForm.subject"
              type="text"
              name="subject"
              placeholder="Worum geht es?"
              required
            />
          </div>

          <div class="form-group">
            <label for="contact-message">Deine Nachricht</label>
            <textarea
              id="contact-message"
              v-model="contactForm.message"
              name="message"
              rows="5"
              placeholder="Schreib uns deine Nachricht ..."
              required
            ></textarea>
          </div>

          <button type="submit" class="contact-submit">
            Nachricht senden
            <span aria-hidden="true">?</span>
          </button>

          <p v-if="contactFeedback" class="contact-feedback" role="status">
            {{ contactFeedback }}
          </p>

          <p class="contact-privacy">
            Hinweis: Das Formular ist noch nicht mit einem E-Mail-Versand
            verbunden. Deine Eingaben werden aktuell nicht übertragen.
          </p>
        </form>
      </div>
    </section>
  </main>
</template>

<style scoped>
.home {
  --navy: #202d43;
  --cream: #f5f1e8;
  --white: #ffffff;
  --sage: #a4aa91;
  --terracotta: #b98d69;
  --text: #343b40;
  --muted: #747a78;

  color: var(--navy);
  background: var(--white);
}

/* Shared typography */
.eyebrow {
  margin: 0 0 1.2rem;
  color: #858b70;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.16em;
}

/* Hero */
.hero {
  padding: 0;
  overflow: hidden;
  background: var(--cream);
}

.hero-content {
  display: grid;
  grid-template-columns: minmax(0, 0.95fr) minmax(0, 1.05fr);
  align-items: center;
  gap: 0;
  width: 100%;
  max-width: none;
  margin: 0;
}

.hero-copy {
  position: relative;
  z-index: 2;
  padding: 3rem 0 3rem 7%;
  margin-right: -5rem;
}

.hero-copy h1 {
  max-width: 680px;
  margin: 0;
  font-size: clamp(2.8rem, 5vw, 5.3rem);
  font-weight: 500;
  letter-spacing: -0.065em;
  line-height: 1.08;
}

.hero-copy h1 span,
.section-heading h2 span,
.sustainability-copy h2 span,
.events-preview-heading h2 span {
  display: block;
  color: #81896f;
}

.hero-description {
  max-width: 470px;
  margin: 1.8rem 0 2rem;
  color: var(--muted);
  font-size: 1rem;
  line-height: 1.85;
}

.primary-link {
  display: inline-flex;
  align-items: center;
  gap: 2rem;
  padding: 1rem 1.4rem;
  background: var(--navy);
  color: var(--white);
  font-size: 0.85rem;
  font-weight: 600;
  text-decoration: none;
  transition: background 0.2s ease, gap 0.2s ease;
}

.primary-link:hover {
  gap: 2.5rem;
  background: #394b64;
}

/* Hero image with soft fade */
.hero-image {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: center;
}

.hero-visual::before {
  position: absolute;
  z-index: 1;
  inset: 0;
  background: linear-gradient(
    to right,
    var(--cream) 0%,
    rgb(245 241 232 / 85%) 12%,
    rgb(245 241 232 / 35%) 32%,
    transparent 58%
  );
  content: "";
  pointer-events: none;
}

.hero-image {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
  object-position: center;
}

/* Products */
.products-section {
  max-width: 1500px;
  margin: 0 auto;
  padding: 5rem 7%;
}

.section-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 3rem;
  margin-bottom: 2.5rem;
}

.section-heading h2 {
  margin: 0;
  font-size: clamp(2.4rem, 4.5vw, 4.2rem);
  font-weight: 500;
  letter-spacing: -0.06em;
  line-height: 1.12;
}

.section-intro {
  max-width: 360px;
  margin: 0 0 0.4rem;
  color: var(--muted);
  font-size: 0.95rem;
  line-height: 1.85;
}

/* Two equal product columns */
.product-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 2rem;
}

.product-card {
  min-width: 0;
}

/* Product visuals */
.product-visual {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 480px;
  overflow: hidden;
  isolation: isolate;
}

/* PRINT placeholder */
.print-visual {
  background:
    radial-gradient(ellipse at 50% 45%, rgb(255 255 255 / 65%), transparent 48%),
    linear-gradient(135deg, #e7e8df, #d5d9cc);
}

.print-visual::before {
  position: absolute;
  z-index: -1;
  top: 50%;
  left: 50%;
  width: 230px;
  height: 230px;
  border: 1px solid rgb(32 45 67 / 16%);
  border-radius: 34% 66% 55% 45% / 45% 42% 58% 55%;
  background:
    linear-gradient(145deg, rgb(255 255 255 / 75%), rgb(164 170 145 / 30%));
  box-shadow:
    18px 18px 0 rgb(32 45 67 / 5%),
    34px 34px 0 rgb(32 45 67 / 3%);
  content: "";
  transform: translate(-50%, -50%) rotate(-18deg);
}

.print-visual::after {
  position: absolute;
  z-index: -1;
  top: 20%;
  right: 16%;
  width: 75px;
  height: 75px;
  border: 1px solid rgb(32 45 67 / 18%);
  border-radius: 50%;
  content: "";
}

.wood-visual {
  background:
    linear-gradient(135deg, rgb(255 255 255 / 25%), transparent 45%),
    #e5e0d3;
}

.wood-visual::before {
  position: absolute;
  z-index: -1;
  top: 50%;
  left: 50%;
  width: 58%;
  height: 48%;
  border: 1px solid rgb(91 72 52 / 22%);
  border-radius: 3px;
  background:
    repeating-linear-gradient(
      8deg,
      transparent 0,
      transparent 17px,
      rgb(112 87 59 / 9%) 18px,
      transparent 20px
    ),
    linear-gradient(135deg, #cbb99c, #b49d7d 55%, #d8c8ae);
  box-shadow:
    12px 12px 0 rgb(83 66 47 / 9%),
    22px 22px 0 rgb(83 66 47 / 5%);
  content: "";
  transform: translate(-50%, -50%) rotate(-7deg);
}

.wood-visual::after {
  position: absolute;
  z-index: -1;
  right: 17%;
  bottom: 18%;
  width: 90px;
  height: 90px;
  border: 1px solid rgb(91 72 52 / 20%);
  border-radius: 50%;
  content: "";
}

.visual-number {
  position: absolute;
  top: 1.3rem;
  left: 1.3rem;
  color: var(--navy);
  font-size: 0.8rem;
  font-weight: 600;
}

.visual-category {
  position: absolute;
  top: 1.5rem;
  right: 1.3rem;
  color: #626a60;
  font-size: 0.62rem;
  letter-spacing: 0.1em;
}

.visual-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1.5rem;
  color: var(--navy);
  text-align: center;
}

.visual-placeholder span {
  font-size: clamp(2.5rem, 5vw, 4.5rem);
  font-weight: 600;
  letter-spacing: -0.06em;
}

.visual-placeholder small {
  color: #656c66;
  font-size: 0.75rem;
}

/* Product information */
.product-info {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding-top: 1.5rem;
}

.product-info .eyebrow {
  margin-bottom: 0.6rem;
}

.product-info h3 {
  margin: 0 0 0.7rem;
  font-size: clamp(1.25rem, 1.7vw, 1.7rem);
  font-weight: 500;
  letter-spacing: -0.04em;
}

.product-info p:last-child {
  max-width: 400px;
  margin: 0;
  color: var(--muted);
  font-size: 0.88rem;
  line-height: 1.8;
}

.product-arrow {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 38px;
  height: 38px;
  border: 1px solid #d8d9d1;
  border-radius: 50%;
  transition: color 0.2s ease, background 0.2s ease;
}

.product-card:hover .product-arrow {
  color: white;
  background: var(--navy);
}

/* Sustainability */
.sustainability-section {
  padding: 4.5rem 7%;
  background: #e9ece3;
}

.sustainability-content {
  display: grid;
  grid-template-columns: 0.9fr 1.1fr;
  align-items: center;
  gap: 3.5rem;
  max-width: 1350px;
  margin: 0 auto;
}

.sustainability-copy h2 {
  margin: 0;
  font-size: clamp(2.8rem, 4.5vw, 4.5rem);
  font-weight: 500;
  letter-spacing: -0.06em;
  line-height: 1.1;
}

.sustainability-description {
  max-width: 490px;
  margin: 1.5rem 0 0;
  color: #626b63;
  font-size: 0.95rem;
  line-height: 1.9;
}

.sustainability-link {
  display: inline-flex;
  align-items: center;
  gap: 1.5rem;
  margin-top: 2rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #858b70;
  color: var(--navy);
  font-size: 0.85rem;
  font-weight: 600;
  text-decoration: none;
  transition: gap 0.2s ease;
}

.sustainability-link:hover {
  gap: 2rem;
}

.sustainability-visual {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 430px;
  overflow: hidden;
  background: #d4dbcd;
}

.sustainability-image-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  color: #455442;
  text-align: center;
}

.recycling-symbol {
  font-size: 7rem;
  font-weight: 300;
  line-height: 1;
}

.sustainability-image-placeholder p {
  margin: 1.5rem 0 0.5rem;
  font-size: 1.1rem;
  font-weight: 700;
  letter-spacing: 0.2em;
}

.sustainability-image-placeholder span:last-child {
  font-size: 0.8rem;
}

.sustainability-caption {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  padding: 1.2rem 1.4rem;
  background: rgb(32 45 67 / 75%);
  color: white;
  font-size: 0.72rem;
  letter-spacing: 0.05em;
}

/* Quiz teaser */
.quiz-teaser {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
  max-width: 1350px;
  margin: 3rem auto 0;
  padding: 2.5rem 3rem;
  background: #f5f1e8;
}

.quiz-teaser h3 {
  max-width: 650px;
  margin: 0;
  font-size: clamp(1.5rem, 2.5vw, 2.2rem);
  font-weight: 500;
  letter-spacing: -0.04em;
}

.quiz-teaser p:last-child {
  max-width: 600px;
  margin: 0.8rem 0 0;
  color: var(--muted);
  font-size: 0.9rem;
  line-height: 1.8;
}

.quiz-decoration {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 90px;
  height: 90px;
  border: 1px solid #a4aa91;
  border-radius: 50%;
  color: #81896f;
  font-family: Georgia, serif;
  font-size: 3.5rem;
}

/* Upcoming events */
.events-preview-section {
  max-width: 1500px;
  margin: 0 auto;
  padding: 5rem 7%;
}

.events-preview-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 2rem;
  margin-bottom: 2.5rem;
}

.events-preview-heading h2 {
  margin: 0;
  font-size: clamp(2.4rem, 4.5vw, 4.2rem);
  font-weight: 500;
  letter-spacing: -0.06em;
  line-height: 1.12;
}

.all-events-link {
  display: inline-flex;
  align-items: center;
  gap: 1.2rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #a4aa91;
  color: var(--navy);
  font-size: 0.85rem;
  font-weight: 600;
  text-decoration: none;
  white-space: nowrap;
  transition: gap 0.2s ease;
}

.all-events-link:hover {
  gap: 1.7rem;
}

.events-preview-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1.2rem;
}

.event-preview-card {
  position: relative;
  display: flex;
  align-items: flex-start;
  gap: 1.2rem;
  min-width: 0;
  min-height: 190px;
  padding: 1.5rem;
  border: 1px solid #e5e4dc;
  background: #faf9f5;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.event-preview-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 10px 25px rgb(32 45 67 / 7%);
}

.event-date {
  display: flex;
  flex-shrink: 0;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 58px;
  height: 68px;
  background: #e9ece3;
  color: var(--navy);
}

.event-day {
  font-size: 1.7rem;
  font-weight: 600;
  line-height: 1;
}

.event-month {
  margin-top: 0.35rem;
  color: #747a68;
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.1em;
}

.event-details {
  min-width: 0;
  padding-right: 0.5rem;
}

.event-details h3 {
  margin: 0.1rem 0 0.8rem;
  font-size: 1.1rem;
  font-weight: 600;
  line-height: 1.4;
}

.event-meta {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  color: var(--muted);
  font-size: 0.78rem;
  line-height: 1.5;
}

.event-meta span span {
  display: inline-block;
  width: 18px;
  color: #858b70;
}

.event-description {
  margin: 0.8rem 0 0;
  color: #747a78;
  font-size: 0.8rem;
  line-height: 1.6;
}

.event-card-arrow {
  position: absolute;
  right: 1.2rem;
  bottom: 1rem;
  color: #858b70;
  font-size: 1rem;
}

.no-events {
  padding: 2.5rem;
  background: #f5f1e8;
  text-align: center;
}

.no-events p {
  margin: 0;
  font-weight: 600;
}

.no-events span {
  display: block;
  margin-top: 0.5rem;
  color: var(--muted);
  font-size: 0.85rem;
}

.events-preview-footer {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  margin-top: 1.5rem;
  padding-top: 1.2rem;
  border-top: 1px solid #e5e4dc;
  color: var(--muted);
  font-size: 0.78rem;
}

.events-preview-footer a {
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
  color: var(--navy);
  font-weight: 600;
  text-decoration: none;
}

/* About us */
.about-section {
  padding: 5rem 7%;
  background: var(--cream);
}

.about-content {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: center;
  gap: 5rem;
  max-width: 1350px;
  margin: 0 auto;
}

.about-visual {
  min-width: 0;
}

.about-image-placeholder {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 480px;
  overflow: hidden;
  background:
    radial-gradient(circle at 25% 30%, rgb(255 255 255 / 45%), transparent 35%),
    linear-gradient(145deg, #c9cdbb, #9da68f);
  color: var(--navy);
}

.about-image-label {
  position: absolute;
  top: 1.5rem;
  left: 1.5rem;
  font-size: 0.65rem;
  font-weight: 700;
  letter-spacing: 0.12em;
}

.about-image-symbol {
  display: flex;
  align-items: flex-end;
  justify-content: center;
  gap: 0.7rem;
  height: 130px;
  margin-bottom: 1.5rem;
}

.about-image-symbol span {
  display: block;
  width: 55px;
  border-radius: 50% 50% 10px 10px;
  background: rgb(32 45 67 / 65%);
}

.about-image-symbol span:nth-child(1) {
  height: 85px;
}

.about-image-symbol span:nth-child(2) {
  height: 120px;
  background: rgb(32 45 67 / 85%);
}

.about-image-symbol span:nth-child(3) {
  height: 95px;
}

.about-image-text {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.4rem;
}

.about-image-text span {
  font-size: 0.7rem;
  letter-spacing: 0.2em;
}

.about-image-text strong {
  font-size: clamp(2rem, 4vw, 3.5rem);
  font-weight: 600;
  letter-spacing: -0.06em;
}

.about-image-caption {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  padding: 1.2rem;
  background: rgb(32 45 67 / 75%);
  color: white;
  font-size: 0.7rem;
  letter-spacing: 0.05em;
  text-align: center;
}

.about-copy h2 {
  margin: 0;
  font-size: clamp(2.8rem, 4.5vw, 4.5rem);
  font-weight: 500;
  letter-spacing: -0.06em;
  line-height: 1.1;
}

.about-copy h2 span {
  display: block;
  color: #81896f;
}

.about-intro {
  margin: 1.5rem 0 0;
  color: var(--navy);
  font-size: 1.05rem;
  font-weight: 500;
  line-height: 1.8;
}

.about-description {
  margin: 1rem 0 0;
  color: var(--muted);
  font-size: 0.9rem;
  line-height: 1.9;
}

.about-values {
  display: flex;
  flex-wrap: wrap;
  gap: 0.7rem;
  margin-top: 2rem;
}

.about-values span {
  padding: 0.7rem 1rem;
  border: 1px solid #c9cdbb;
  color: #626b63;
  font-size: 0.72rem;
  font-weight: 600;
  letter-spacing: 0.04em;
}

/* Contact section */
.contact-section {
  padding: 5rem 7%;
  background: #ffffff;
}

.contact-content {
  display: grid;
  grid-template-columns: 0.85fr 1.15fr;
  align-items: start;
  gap: 5rem;
  max-width: 1350px;
  margin: 0 auto;
}

.contact-copy h2 {
  margin: 0;
  font-size: clamp(2.8rem, 4.5vw, 4.5rem);
  font-weight: 500;
  letter-spacing: -0.06em;
  line-height: 1.1;
}

.contact-copy h2 span {
  display: block;
  color: #81896f;
}

.contact-description {
  max-width: 430px;
  margin: 1.5rem 0 0;
  color: var(--muted);
  font-size: 0.95rem;
  line-height: 1.9;
}

.contact-note {
  display: flex;
  align-items: flex-start;
  gap: 1rem;
  max-width: 430px;
  margin-top: 2rem;
  padding: 1.2rem;
  background: var(--cream);
}

.contact-note-symbol {
  color: #858b70;
  font-size: 1.3rem;
}

.contact-note p {
  margin: 0;
  color: #626b63;
  font-size: 0.82rem;
  line-height: 1.7;
}

/* Contact form */
.contact-form {
  display: flex;
  flex-direction: column;
  gap: 1.2rem;
  padding: 2rem;
  background: #f8f7f2;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.2rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
}

.form-group label {
  color: var(--navy);
  font-size: 0.8rem;
  font-weight: 600;
}

.form-group input,
.form-group textarea {
  width: 100%;
  box-sizing: border-box;
  padding: 0.9rem 1rem;
  border: 1px solid #deded5;
  border-radius: 0;
  outline: none;
  background: #ffffff;
  color: var(--navy);
  font: inherit;
  font-size: 0.85rem;
  transition: border-color 0.2s ease, box-shadow 0.2s ease;
}

.form-group input {
  min-height: 48px;
}

.form-group textarea {
  min-height: 140px;
  resize: vertical;
}

.form-group input::placeholder,
.form-group textarea::placeholder {
  color: #a1a49d;
}

.form-group input:focus,
.form-group textarea:focus {
  border-color: #858b70;
  box-shadow: 0 0 0 2px rgb(133 139 112 / 12%);
}

.contact-submit {
  display: inline-flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
  align-self: flex-start;
  min-width: 210px;
  padding: 1rem 1.3rem;
  border: 0;
  background: var(--navy);
  color: #ffffff;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s ease, gap 0.2s ease;
}

.contact-submit:hover {
  gap: 2.5rem;
  background: #394b64;
}

.contact-feedback {
  margin: 0;
  color: #626b63;
  font-size: 0.85rem;
  line-height: 1.6;
}

.contact-privacy {
  margin: 0;
  color: #858b86;
  font-size: 0.72rem;
  line-height: 1.6;
}

/* Responsive */
@media (max-width: 1000px) {
  .hero-content {
    grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  }

  .hero-copy {
    margin-right: -3rem;
  }

  .hero-visual {
    height: 430px;
    max-height: 430px;
  }

  .hero {
    margin-top: 0;
    padding-top: 0;
  }

  .hero-content {
    margin-top: 0;
  }

  .product-visual {
    min-height: 380px;
  }

  .sustainability-content {
    gap: 2.5rem;
  }

  .sustainability-visual {
    min-height: 360px;
  }

  .events-preview-grid {
    grid-template-columns: 1fr;
  }

  .event-preview-card {
    min-height: auto;
  }

  .about-content {
    gap: 2.5rem;
  }

  .about-image-placeholder {
    min-height: 400px;
  }

  .contact-content {
    gap: 2.5rem;
  }
}

@media (max-width: 700px) {
  .hero {
    padding: 0;
  }

  .hero-content {
    grid-template-columns: 1fr;
    gap: 0;
  }

  .hero-copy {
    padding: 1.5rem 1.25rem 2rem;
    margin-right: 0;
  }

  .hero-copy h1 {
    font-size: clamp(2.6rem, 11vw, 4rem);
  }

  .hero-visual {
    display: none;
  }

  .products-section {
    padding: 3.5rem 1.25rem;
  }
}

@media (max-width: 450px) {
  .quiz-teaser {
    flex-direction: column-reverse;
    gap: 1rem;
  }

  .form-row {
    grid-template-columns: 1fr;
  }

  .visual-category {
    font-size: 0.55rem;
  }

  .print-visual::before {
    width: 180px;
    height: 180px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .primary-link,
  .product-arrow,
  .sustainability-link,
  .event-preview-card,
  .all-events-link,
  .contact-submit {
    transition: none;
  }
}
</style>
