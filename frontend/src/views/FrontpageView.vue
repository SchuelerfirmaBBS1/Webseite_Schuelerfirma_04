<script setup lang="ts">
import { computed, ref } from 'vue'
import { events } from '../data/events'

/* Convert a date in DD.MM.YYYY format into a Date object */
function parseEventDate(dateString: string): Date {
  const [day = 1, month = 1, year = 1970] = dateString
    .split('.')
    .map(Number)

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
  return dateString.split('.')[0] ?? ''
}

function getMonth(dateString: string): string {
  const month = Number(dateString.split('.')[1] ?? '0')

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

    <!-- Left side: Introduction -->
    <div class="hero-copy">

      <div class="hero-logo-frame">
       <img
          src="/images/white_logo.png"
          alt="PRINT&BITE"
          class="hero-logo"
        />
</div>

      <h1>
        Aus Ideen wird
        <span>etwas Besonderes.</span>
      </h1>

      <p class="hero-description">
        Von nachhaltigen 3D-Druckideen über handwerkliche Holzprodukte
        bis hin zu frisch gebackenen Snacks: Bei PRINT&BITE verbinden
        wir Kreativität, Handwerk und Genuss. Als Schülerunternehmen
        der BBS I Lüneburg machen wir aus unseren Ideen echte Produkte.
      </p>

      <div class="hero-actions">
        <a href="#produkte" class="primary-link">
          Unsere Produkte entdecken
          <span aria-hidden="true">↗</span>
        </a>

        <a href="#ueber-uns" class="secondary-link">
          Unser Team kennenlernen
        </a>
      </div>

      <div class="hero-bottom-note">
        <span>HA26B</span>
        <span class="hero-bottom-line"></span>
        <span>Gemeinsam kreativ. Gemeinsam erfolgreich.</span>
      </div>

    </div>

    <!-- Right side: Creative product collage -->
    <div class="hero-visual" aria-label="Einblicke in die Produkte von PRINT&BITE">

      <div class="hero-collage">

        <!-- Decorative elements -->
        <div class="collage-circle"></div>
        <div class="collage-sparkle" aria-hidden="true">✳</div>
        <div class="collage-small-note">Mit Ideen gemacht!</div>

        <!-- PRINT -->
        <div class="collage-print">
          <img
            src="\images\3D-Druck.png"
          />
          <span class="collage-label">PRINT</span>
        </div>

        <!-- HANDWERK -->
        <div class="collage-wood">
          <img
            src="\images\Handwerk-Ente.png"
            alt="Beispiel eines handgefertigten Holzbretts"
          />
          <span class="collage-label">HANDWERK</span>
        </div>

        <!-- BITE -->
        <div class="collage-bite">
          <img
            src="https://static.spotapps.co/spots/ae/bfd00975a4485dac6078c470ba5be5/full"
            alt="Beispiel von frisch gebackenen Pizzaschnecken"
          />
          <span class="collage-label">BITE</span>
        </div>

        <div class="collage-sticker">
          <span>IDEEN</span>
          <strong>WERDEN</strong>
          <span>REAL.</span>
        </div>

      </div>

    </div>
  </div>
</section>



<!-- Product teaser -->
<section id="produkte" class="products-section">
  <div class="products-teaser-container">

    <div class="products-teaser-heading">
      <div>
        <span class="section-eyebrow">UNSERE PRODUKTWELT</span>
        <h2>
          Kreative Ideen.
          <span>Handgemacht & durchdacht.</span>
        </h2>
      </div>

      <p>
        Von nachhaltigen 3D-Druck-Produkten bis zu handgefertigten
        Holzarbeiten – bei uns treffen verschiedene Ideen und
        Fähigkeiten aufeinander.
      </p>
    </div>

    <div class="products-teaser-grid">

      <!-- PRINT -->
      <article class="products-teaser-card print-teaser">
        <div class="teaser-visual">
          <img
            class="teaser-image"
            src="\images\3D-Druck.png"
            alt="3D-gedrucktes Objekt neben einem 3D-Drucker"
            loading="lazy"
          />
          <div class="teaser-image-overlay"></div>

          <span class="teaser-number">01 / PRINT</span>
          <span class="teaser-decoration print-decoration">✳</span>
          <span class="teaser-word">PRINT</span>
          <span class="teaser-tag">TECHNOLOGIE & NACHHALTIGKEIT</span>
        </div>

        <div class="teaser-info">
          <div>
            <h3>Aus Ideen wird etwas Greifbares.</h3>
            <p>
              Wir verwandeln recycelten Kunststoff in neue,
              praktische und kreative 3D-Druck-Produkte.
            </p>
          </div>
          <span class="teaser-arrow" aria-hidden="true">↗</span>
        </div>
      </article>

      <!-- HANDWERK -->
      <article class="products-teaser-card handwerk-teaser">
        <div class="teaser-visual">
          <img
            class="teaser-image"
            src="\images\Handwerk.png"
            alt="Handgefertigte Holzfiguren und Holzarbeiten"
            loading="lazy"
          />
          <div class="teaser-image-overlay"></div>

          <span class="teaser-number">02 / HANDWERK</span>
          <span class="teaser-decoration handwerk-decoration">✳</span>
          <span class="teaser-word">HANDWERK</span>
          <span class="teaser-tag">KOOPERATION MIT DER BBS II</span>
        </div>

        <div class="teaser-info">
          <div>
            <h3>Mit Liebe zum Detail gefertigt.</h3>
            <p>
              Gemeinsam mit der BBS II bieten wir handgefertigte
              Holzprodukte an, bei denen Kreativität und
              handwerkliches Können zusammenkommen.
            </p>
          </div>
          <span class="teaser-arrow" aria-hidden="true">↗</span>
        </div>
      </article>

    </div>

    <!-- BITE -->
    <div class="products-bite-note">
      <img
        class="bite-note-image"
        src="https://images.unsplash.com/photo-1781722758469-975dd9e90643?auto=format&fit=crop&w=500&q=85"
        alt="Gebackene herzhafte Röllchen"
        loading="lazy"
      />

      <div class="bite-note-copy">
        <span class="bite-note-label">UND NATÜRLICH AUCH BITE</span>
        <p>
          Neben unseren Produkten gibt es bei uns auch selbstgemachte
          Snacks und Backwaren – frisch zubereitet und mit viel Freude
          gemacht.
        </p>
      </div>

      <span class="bite-note-word">BITE</span>
    </div>

  </div>
</section>



<!-- Sustainability section -->
<section class="sustainability-section">
  <div class="sustainability-layout">
    <div class="sustainability-copy">
      <div class="sustainability-kicker">
        <span></span>
        NACHHALTIGKEIT · NEU GEDACHT
      </div>

      <h2>
        Aus Alt wird
        <span>etwas Neues.</span>
      </h2>

      <p class="sustainability-lead">
        Nachhaltigkeit beginnt bei den Materialien.
      </p>

      <p class="sustainability-description">
        Für unsere PRINT-Produkte verwenden wir recyceltes
        Kunststofffilament. So werden bereits verwendete
        Kunststoffe zur Grundlage für neue kreative Ideen.
      </p>

      <p class="sustainability-description">
        Wie aus Kunststoffresten wieder neues Filament entstehen
        kann, zeigt beispielsweise die Recyclingfabrik aus
        Braunschweig.
      </p>

      <a
        href="https://www.recyclingfabrik.com/"
        target="_blank"
        rel="noopener noreferrer"
        class="sustainability-link"
      >
        <span>Die Recyclingfabrik kennenlernen</span>
        <span aria-hidden="true">↗</span>
      </a>
    </div>

    <div class="recycling-process">
      <div class="process-heading">
        <span>VOM DRUCKREST ZUR NEUEN ROLLE</span>
        <span>IN DREI SCHRITTEN</span>
      </div>

      <div class="process-grid">
        <article class="process-card">
          <div class="process-image">
            <img
              src="https://images.ndr.de/image/beb0e5cd-9b94-4fdb-a73b-5449fd8bafde/AAABlb3YzvM/AAABnR8VW9w/original/recyclingfabrik102.jpg"
              alt="Gesammelte und zerkleinerte Reste aus dem 3D-Druck"
              loading="lazy"
            />
            <span class="process-number">01</span>
          </div>

          <div class="process-info">
            <span class="process-step-label">SAMMELN</span>
            <h3>Kunststoffreste</h3>
            <p>
              Fehldrucke und andere Reste aus dem 3D-Druck
              werden gesammelt.
            </p>
          </div>
        </article>

        <article class="process-card">
          <div class="process-image">
            <img
              src="https://die-region.de/fileadmin/user_upload/blogbeitraege/2022/12_Dezember/DSCF0080.jpg"
              alt="Kleine Kunststoffstücke nach der Zerkleinerung"
              loading="lazy"
            />
            <span class="process-number">02</span>
          </div>

          <div class="process-info">
            <span class="process-step-label">AUFBEREITEN</span>
            <h3>Neues Granulat</h3>
            <p>
              Das Material wird sortiert und zerkleinert,
              sodass es weiterverarbeitet werden kann.
            </p>
          </div>
        </article>

        <article class="process-card">
          <div class="process-image">
            <img
              src="https://colorfabb.com/media/catalog/product/c/o/color_per_batch4.jpg?bg-color=255%2C255%2C255&canvas=500%3A500&fit=bounds&height=500&optimize=medium&width=500"
              alt="Rolle mit recyceltem Kunststofffilament"
              loading="lazy"
            />
            <span class="process-number">03</span>
          </div>

          <div class="process-info">
            <span class="process-step-label">WIEDERVERWENDEN</span>
            <h3>Neues Filament</h3>
            <p>
              Aus dem aufbereiteten Kunststoff wird neues
              Filament für den 3D-Druck hergestellt.
            </p>
          </div>
        </article>
      </div>
    </div>
  </div>

  <div class="sustainability-quiz">
    <div class="quiz-copy">
      <span class="quiz-label">WISSEN ENTDECKEN</span>
      <h3>Wie gut kennst du dich mit Plastik und Wasser aus?</h3>
      <p>
        Teste dein Wissen und entdecke spannende Fakten
        rund um Recycling und den bewussten Umgang mit Ressourcen.
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
    <div class="events-heading-copy">
      <p class="eyebrow">TERMINE & VERANSTALTUNGEN</p>
      <h2>
        Wir sehen uns
        <span>demnächst!</span>
      </h2>
      <p class="events-intro">
        Ob Verkaufsaktion, Messe oder besondere Veranstaltung –
        hier erfährst du, wo du PRINT&BITE als Nächstes treffen kannst.
      </p>
    </div>

    <RouterLink to="/termine" class="all-events-link">
      Alle Termine entdecken
      <span aria-hidden="true">↗</span>
    </RouterLink>
  </div>

  <div v-if="upcomingEvents.length > 0" class="events-preview-grid">
    <article
      v-for="(event, index) in upcomingEvents"
      :key="event.name + event.startDate"
      class="event-preview-card"
    >
      <div class="event-card-top">
        <span class="event-card-index">
          0{{ index + 1 }} / 03
        </span>
        <span class="event-card-mark" aria-hidden="true">✳</span>
      </div>

      <div class="event-date">
        <span class="event-day">{{ getDay(event.startDate) }}</span>
        <span class="event-month">{{ getMonth(event.startDate) }}</span>
      </div>

      <div class="event-details">
        <h3>{{ event.name }}</h3>

        <div class="event-meta">
          <span v-if="event.startTime || event.endTime">
            <span class="event-meta-icon" aria-hidden="true">◷</span>
            {{ event.startTime }}
            <template v-if="event.endTime">
              – {{ event.endTime }}
            </template>
          </span>

          <span>
            <span class="event-meta-icon" aria-hidden="true">⌖</span>
            {{ event.location }}
          </span>
        </div>

        <p v-if="event.description" class="event-description">
          {{ event.description }}
        </p>
      </div>

      <div class="event-card-bottom">
        <span>PRINT&BITE · HA26B</span>
        <span aria-hidden="true">↗</span>
      </div>
    </article>
  </div>

  <div v-else class="no-events">
    <span class="no-events-symbol" aria-hidden="true">✳</span>
    <p>Aktuell sind keine kommenden Termine geplant.</p>
    <span>Schau gerne später wieder vorbei!</span>
  </div>

  <div class="events-preview-footer">
    <span>Wir freuen uns darauf, dich bei unseren Veranstaltungen zu sehen.</span>
    <RouterLink to="/termine">
      Zur vollständigen Terminübersicht
      <span aria-hidden="true">↗</span>
    </RouterLink>
  </div>
</section>



<!-- About us section -->
<section id="ueber-uns" class="about-section">
  <div class="about-container">

    <div class="about-intro">
      <div class="about-photo">
        <img
          src="/images/ha26b_image.jpeg"
          alt="Die Klasse HA26B – das Team hinter Print & Bite"
        />
        <span class="about-photo-caption">
          Das Team hinter Print & Bite
        </span>
      </div>

      <div class="about-intro-copy">
        <span class="section-eyebrow">DAS TEAM HINTER PRINT & BITE</span>
        <h2>Wir sind <span>Print & Bite!</span></h2>

        <p>
          Die Schülerfirma der Klasse HA26B an der einjährigen
          Berufsfachschule Wirtschaft mit dem Schwerpunkt Handel und IT
          an der BBS 1 in Lüneburg.
        </p>

        <p class="about-name">
          Der Name ist bei uns Programm:
        </p>

        <p>
          Wir verbinden moderne Technologie mit leckerem Genuss und
          handgemachter Qualität.
        </p>
      </div>
    </div>

    <div class="about-who">
      <span class="section-eyebrow">WER WIR SIND</span>
      <h3>Eine Klasse. Ein Unternehmen. Viele Ideen.</h3>

      <p>
        Hinter Print & Bite steht die gesamte Klasse HA26B. Als angehende
        Spezialisten für Handel und IT lernen wir Wirtschaft nicht nur aus
        Büchern, sondern führen unser eigenes reales Unternehmen. Von der
        technischen Umsetzung im 3D-Druck über das Backen in der Küche bis
        hin zum Verkauf und dem Marketing liegt alles komplett in unserer Hand.
      </p>
    </div>

    <div class="about-areas">
      <div class="about-areas-heading">
        <span class="section-eyebrow">WAS WIR MACHEN</span>
        <h3>Drei Bereiche, eine gemeinsame Idee.</h3>
        <p>
          Unsere Schülerfirma teilt sich in drei kreative und nachhaltige
          Bereiche auf:
        </p>
      </div>

      <div class="about-area-grid">
        <article class="about-area-card">
          <span class="about-area-number">01</span>
          <h4>Print</h4>
          <span class="about-area-subtitle">Technologie & Nachhaltigkeit</span>
          <p>
            Wir fertigen moderne Produkte im 3D-Druck, für die wir
            ausschließlich recyceltes Plastik verwenden. Nachhaltigkeit
            steht bei uns im Fokus!
          </p>
        </article>

        <article class="about-area-card">
          <span class="about-area-number">02</span>
          <h4>Bite</h4>
          <span class="about-area-subtitle">Genuss aus eigener Herstellung</span>
          <p>
            Für den perfekten Snack zwischendurch backen wir frische
            Pizzaschnecken selbst und bieten leckere Brezeln an.
          </p>
        </article>

        <article class="about-area-card">
          <span class="about-area-number">03</span>
          <h4>Kooperation & Handwerk</h4>
          <span class="about-area-subtitle">Gemeinsam etwas schaffen</span>
          <p>
            In enger Zusammenarbeit mit der BBS 2 vertreiben wir außerdem
            hochwertige handwerklich Holzprodukte.
          </p>
        </article>
      </div>
    </div>

    <div class="about-why">
      <div class="about-why-heading">
        <span class="section-eyebrow">WARUM WIR DAS MACHEN</span>
        <h3>Mehr als nur Unterricht.</h3>
      </div>

      <div class="about-why-copy">
        <p>
          Durch den Schwerpunkt Handel und IT verbinden wir kaufmännisches
          Wissen mit digitaler Praxis. Wir lernen, wie man Produkte
          kalkuliert, Kunden begeistert und nachhaltige Prozesse umsetzt.
          Die Schülerfirma ist für uns als Klasse HA26B der perfekte Start
          in die berufliche Zukunft, bei dem wir als Team zusammenwachsen
          und echte Verantwortung übernehmen.
        </p>

        <p class="about-closing">
          Unterstützt unsere Schülerfirma und probiert unsere Produkte –
          wir freuen uns auf euch!
        </p>
      </div>
    </div>

  </div>
</section>
  </main>
</template>


<style scoped src="../assets/css/home/home.css"></style>
<style scoped src="../assets/css/home/hero.css"></style>
<style scoped src="../assets/css/home/products.css"></style>
<style scoped src="../assets/css/home/sustainability.css"></style>
<style scoped src="../assets/css/home/events.css"></style>
<style scoped src="../assets/css/home/about.css"></style>

<style scoped>
/* Responsive */
@media (max-width: 1000px) {

.hero {
  padding-right: 5%;
  padding-left: 5%;
}

.hero-content {
  gap: 2rem;
}

.hero-composition {
  min-height: 390px;
}

.hero-card {
  width: 190px;
  height: 205px;
  padding: 1.1rem;
}

.hero-card-image {
  height: 90px;
}

.hero-composition {
  min-height: 430px;
}
.hero-card strong {
  font-size: 1.7rem;
}

.hero-shape-back {
  width: 220px;
  height: 220px;
}

.product-visual {
  min-height: 350px;
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
  padding: 2.5rem 1.25rem;
}

.hero-content {
  grid-template-columns: 1fr;
  gap: 2.5rem;
}

.hero-copy {
  padding: 0;
}

.hero-copy h1 {
  font-size: clamp(2.8rem, 12vw, 4.5rem);
}

.hero-bottom-note {
  margin-top: 2.5rem;
}

.hero-visual {
  width: auto;
  max-width: 500px;
  margin: 0 auto;
  transform: none;
}

.hero-composition {
  min-height: 390px;
}
.hero-card {
  width: 180px;
  height: 195px;
  padding: 1rem;
}

.hero-card-image {
  height: 85px;
}


  .products-section {
    padding: 3.5rem 1.25rem;
  }

.product-grid {
  grid-template-columns: 1fr;
  gap: 2rem;
}

.product-visual {
  min-height: 350px;
}
}

@media (max-width: 450px) {

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
