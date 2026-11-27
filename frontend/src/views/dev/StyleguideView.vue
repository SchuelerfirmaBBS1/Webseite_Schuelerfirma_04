<script setup lang="ts">
/**
 * Living style guide: every design token and base component in all variants.
 * Only available on the dev server (/styleguide), never in production builds.
 */
import { reactive, ref } from 'vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseModal from '@/components/base/BaseModal.vue'
import BaseField from '@/components/base/BaseField.vue'
import BaseInput from '@/components/base/BaseInput.vue'
import BaseTextarea from '@/components/base/BaseTextarea.vue'
import BaseSelect from '@/components/base/BaseSelect.vue'
import BaseCheckbox from '@/components/base/BaseCheckbox.vue'
import BaseSection from '@/components/base/BaseSection.vue'
import BaseHeading from '@/components/base/BaseHeading.vue'
import BaseImage from '@/components/base/BaseImage.vue'
import BaseFrame from '@/components/base/BaseFrame.vue'
import BaseMediaText from '@/components/base/BaseMediaText.vue'
import BaseCard from '@/components/base/BaseCard.vue'
import BaseTag from '@/components/base/BaseTag.vue'
import BaseBlob from '@/components/base/BaseBlob.vue'
import BaseSticker from '@/components/base/BaseSticker.vue'
import BaseDecor from '@/components/base/BaseDecor.vue'

// One sample photo for every image demo (from the project documentation), only bundled with this dev page
import samplePhoto from './images/holzprodukte.jpg'

const colors = [
  { name: 'Blau (Primär)', token: '--color-blue', use: 'Buttons, Hervorhebungen, Schatten' },
  { name: 'Dunkelblau (Kontrast)', token: '--color-ink', use: 'Alle Texte, Header, Footer, Rahmen' },
  { name: 'Grau (Sekundär)', token: '--color-grey', use: 'Linien, Platzhalter, nie für Fließtext' },
  { name: 'Salbei (Akzent)', token: '--color-sage', use: 'Labels, kleine Akzente' },
  { name: 'Terrakotta (Signal)', token: '--color-terracotta', use: 'Call-to-Actions, Sterne, Sticker' },
  { name: 'Creme', token: '--color-cream', use: 'Seitenhintergrund' },
  { name: 'Weiß', token: '--color-white', use: 'Karten, Felder, helle Abschnitte' },
  { name: 'Fehler', token: '--color-error', use: 'Nur Formularfehler' },
]

const typeScale = [
  { token: '--text-2xl', label: 'Seitentitel', heading: true },
  { token: '--text-xl', label: 'Abschnittstitel', heading: true },
  { token: '--text-lg', label: 'Kartentitel', heading: true },
  { token: '--text-md', label: 'Fließtext', heading: false },
  { token: '--text-sm', label: 'Kleintext, Labels', heading: false },
  { token: '--text-xs', label: 'Eyebrow, Tags', heading: false },
]

const spacing = [
  { token: '--space-xs', use: 'Innerhalb kleiner Elemente: Label zu Feld, Tags' },
  { token: '--space-sm', use: 'Zwischen zusammengehörigen Elementen' },
  { token: '--space-md', use: 'Zwischen Blöcken in einer Karte oder einem Formular' },
  { token: '--space-lg', use: 'Zwischen Gruppen, kleine Abschnitte' },
  { token: '--space-xl', use: 'Abschnitte, Bild zu Text' },
]

const radii = [
  { token: '--radius-sm', label: 'Felder, Sticker' },
  { token: '--radius-md', label: 'Bilder' },
  { token: '--radius-organic', label: 'Karten, Modals' },
  { token: '--radius-pill', label: 'Buttons, Tags' },
]

const topics = [
  { value: 'products', label: 'Frage zu Produkten' },
  { value: 'event', label: 'Termin oder Verkaufsstand' },
  { value: 'other', label: 'Etwas anderes' },
]

const form = reactive({ name: '', email: 'keine-mail', topic: '', message: '', consent: false })
const modalOpen = ref(false)
const smallModalOpen = ref(false)
const submitting = ref(false)

function fakeSubmit() {
  submitting.value = true
  setTimeout(() => (submitting.value = false), 1500)
}
</script>

<template>
  <main class="styleguide">
    <!-- Intro -->
    <BaseSection as="header" tone="cream">
      <div class="intro">
        <BaseHeading
          :level="1"
          eyebrow="Nur im Dev-Server"
          title="Styleguide"
          highlight="Print & Bite."
        >
          <p>
            Alle festen Stilregeln an einem Ort. Neue Seiten werden nur aus diesen Bausteinen und den
            Design-Tokens (<code>src/assets/css/tokens.css</code>) gebaut, keine eigenen Farben,
            Abstände oder Schriftgrößen.
          </p>
        </BaseHeading>

        <div class="intro__art">
          <BaseBlob color="white" offset class="intro__blob" />
          <BaseImage
            :src="samplePhoto"
            alt="Holzprodukte der Schülerfirma"
            frame="blob"
            ratio="1 / 1"
            class="intro__image"
          />
          <BaseDecor kind="star" spin class="intro__rays" />
          <BaseDecor kind="dots" color="blue" class="intro__dots" />
          <BaseSticker class="intro__sticker">Mit Ideen gemacht!</BaseSticker>
        </div>
      </div>
    </BaseSection>

    <!-- Colours -->
    <BaseSection tone="light" spacing="md">
      <BaseHeading eyebrow="Tokens" title="Farben" :level="2">
        <p>
          Sieben Farben plus Fehlerrot, mehr nicht. Text ist immer Dunkelblau; Terrakotta ist die
          einzige Signalfarbe.
        </p>
      </BaseHeading>
      <ul class="swatches">
        <li v-for="color in colors" :key="color.token" class="swatch">
          <span class="swatch__color" :style="{ background: `var(${color.token})` }"></span>
          <strong>{{ color.name }}</strong>
          <code>{{ color.token }}</code>
          <small>{{ color.use }}</small>
        </li>
      </ul>
    </BaseSection>

    <!-- Typography -->
    <BaseSection tone="cream" spacing="md">
      <BaseHeading eyebrow="Tokens" title="Typografie" :level="2">
        <p>
          Überschriften wie im Hero der Startseite: kräftige Serifenschrift mit engem Zeichenabstand
          (<code>--font-display</code>, Georgia, später Best Swashed). Text und UI:
          <code>--font-body</code> (Trebuchet MS wie in Header und Footer, später Archibrazo).
        </p>
      </BaseHeading>
      <ul class="type-scale">
        <li v-for="step in typeScale" :key="step.token" class="type-scale__row">
          <code>{{ step.token }}</code>
          <span :class="{ 'is-display': step.heading }" :style="{ fontSize: `var(${step.token})` }">
            {{ step.label }}
          </span>
        </li>
      </ul>
    </BaseSection>

    <!-- Spacing, radii, shadows -->
    <BaseSection tone="light" spacing="md">
      <BaseHeading eyebrow="Tokens" title="Abstände, Formen, Schatten" :level="2">
        <p>Fünf Abstände und ein einziger Schatten. Was nicht passt, bekommt keinen neuen Wert.</p>
      </BaseHeading>
      <div class="tokens-grid">
        <div>
          <h3 class="sub-title">Abstände</h3>
          <ul class="spacing">
            <li v-for="space in spacing" :key="space.token">
              <code>{{ space.token }}</code>
              <span class="spacing__bar" :style="{ width: `var(${space.token})` }"></span>
              <small>{{ space.use }}</small>
            </li>
          </ul>
        </div>
        <div>
          <h3 class="sub-title">Radien</h3>
          <ul class="radii">
            <li v-for="radius in radii" :key="radius.token">
              <span class="radii__box" :style="{ borderRadius: `var(${radius.token})` }"></span>
              <code>{{ radius.token }}</code>
              <small>{{ radius.label }}</small>
            </li>
          </ul>
          <h3 class="sub-title">Schatten (hart, ohne Weichzeichner)</h3>
          <div class="shadows">
            <span class="shadows__box">Standard: Blau</span>
            <span class="shadows__box shadows__box--dark">Auf Dunkel: Terrakotta</span>
          </div>
          <p class="note">
            Ein Versatz für alles (<code>--shadow-offset</code>). Die Farbe kommt aus
            <code>--shadow-color</code>: Blau, auf dunklen oder blauen Flächen Terrakotta.
          </p>
        </div>
      </div>
    </BaseSection>

    <!-- Buttons -->
    <BaseSection tone="cream" spacing="md">
      <BaseHeading eyebrow="Komponente" title="Buttons" :level="2">
        <p>
          <code>BaseButton</code>: ein Button für Aktionen und Links (<code>to</code>, <code>href</code>
          oder <code>&lt;button&gt;</code>). Pro Bereich höchstens ein Primary oder Accent.
        </p>
      </BaseHeading>
      <div class="demo-rows">
        <div class="demo-row">
          <BaseButton variant="primary" arrow>Primary</BaseButton>
          <BaseButton variant="accent" arrow>Accent</BaseButton>
          <BaseButton variant="secondary">Secondary</BaseButton>
          <BaseButton variant="text" arrow>Text-Link</BaseButton>
        </div>
        <div class="demo-row">
          <BaseButton variant="primary" size="sm">Primary sm</BaseButton>
          <BaseButton variant="accent" size="sm">Accent sm</BaseButton>
          <BaseButton variant="secondary" size="sm">Secondary sm</BaseButton>
        </div>
        <div class="demo-row">
          <BaseButton variant="primary" disabled>Deaktiviert</BaseButton>
          <BaseButton variant="primary" loading>Wird gesendet</BaseButton>
          <BaseButton variant="secondary" to="/termine" arrow>Als Router-Link</BaseButton>
        </div>
      </div>
    </BaseSection>

    <!-- Forms -->
    <BaseSection tone="light" spacing="md">
      <BaseMediaText>
        <template #media>
          <BaseHeading eyebrow="Komponente" title="Formulare" :level="2">
            <p>
              <code>BaseField</code> liefert Label, Hinweis und Fehler und verknüpft sie automatisch mit
              <code>BaseInput</code>, <code>BaseTextarea</code> oder <code>BaseSelect</code>.
              <code>BaseCheckbox</code> für Einwilligungen.
            </p>
          </BaseHeading>
        </template>

        <form class="demo-form" @submit.prevent="fakeSubmit">
          <div class="demo-form__row">
            <BaseField label="Name" required>
              <BaseInput v-model="form.name" placeholder="Vor- und Nachname" autocomplete="name" />
            </BaseField>
            <BaseField label="E-Mail" required error="Bitte gib eine gültige E-Mail-Adresse ein.">
              <BaseInput v-model="form.email" type="email" />
            </BaseField>
          </div>
          <BaseField label="Thema" hint="Hilft uns, deine Nachricht schneller zu beantworten.">
            <BaseSelect v-model="form.topic" :options="topics" placeholder="Bitte wählen" />
          </BaseField>
          <BaseField label="Nachricht" required>
            <BaseTextarea v-model="form.message" placeholder="Schreib uns deine Nachricht …" />
          </BaseField>
          <BaseCheckbox v-model="form.consent" required>
            Ich bin einverstanden, dass meine Angaben zur Beantwortung meiner Anfrage gespeichert werden.
          </BaseCheckbox>
          <BaseButton type="submit" variant="primary" arrow :loading="submitting" class="demo-form__submit">
            Absenden
          </BaseButton>
        </form>
      </BaseMediaText>
    </BaseSection>

    <!-- Modal -->
    <BaseSection tone="cream" spacing="md">
      <BaseHeading eyebrow="Komponente" title="Modal" :level="2">
        <p>
          <code>BaseModal</code> mit <code>v-model:open</code>. Esc, Klick auf den Hintergrund und das
          × schließen es; der Fokus bleibt im Dialog.
        </p>
      </BaseHeading>
      <div class="demo-row">
        <BaseButton variant="primary" @click="modalOpen = true">Modal öffnen</BaseButton>
        <BaseButton variant="secondary" @click="smallModalOpen = true">Kleines Modal</BaseButton>
      </div>

      <BaseModal v-model:open="modalOpen" eyebrow="Quiz" title="Wie viel Plastik landet im Meer?">
        <p>
          Beispielinhalt: Ein Modal eignet sich für kurze, abgeschlossene Inhalte wie eine Quizfrage,
          eine Bildvergrößerung oder eine Bestätigung.
        </p>
        <template #actions>
          <BaseButton variant="secondary" size="sm" @click="modalOpen = false">Abbrechen</BaseButton>
          <BaseButton variant="primary" size="sm" @click="modalOpen = false">Weiter</BaseButton>
        </template>
      </BaseModal>

      <BaseModal v-model:open="smallModalOpen" size="sm" title="Nachricht gesendet">
        <p>Danke! Wir melden uns bald bei dir.</p>
        <template #actions>
          <BaseButton variant="accent" size="sm" @click="smallModalOpen = false">Alles klar</BaseButton>
        </template>
      </BaseModal>
    </BaseSection>

    <!-- Image and text blocks -->
    <BaseSection tone="light" spacing="md">
      <BaseHeading eyebrow="Komponenten" title="Bild- und Textblöcke" :level="2">
        <p>
          <code>BaseSection</code> (Hintergrund und Container), <code>BaseHeading</code>,
          <code>BaseMediaText</code>, <code>BaseCard</code>, <code>BaseImage</code> und
          <code>BaseTag</code>.
        </p>
      </BaseHeading>

      <h3 class="sub-title">Verspielte Rahmen: BaseFrame</h3>
      <p class="note">
        Mickys Hero-Rahmen: Foto im weißen Passepartout, gegenläufig gedrehte Platte dahinter, leicht
        schräg, optional ein Label. Zwei Formen (<code>arch</code>, <code>round</code>), Platte nur in
        Kernfarben. <strong>Nur für den Hero bzw. Bild-Collagen</strong>, nie in Karten, Galerien oder
        Media + Text (dort Blob + Bild, siehe unten). Liegen zwei Rahmen nebeneinander, abwechselnd <code>tilt</code> left/right.
      </p>
      <div class="frame-grid">
        <BaseFrame :src="samplePhoto" alt="Holzprodukte der Schülerfirma" plate="blue" label="Print" label-tone="terracotta" />
        <BaseFrame :src="samplePhoto" alt="Holzprodukte der Schülerfirma" plate="terracotta" tilt="right" label="Handwerk" />
        <BaseFrame :src="samplePhoto" alt="Holzprodukte der Schülerfirma" shape="round" plate="ink" label="Team" />
        <BaseFrame :src="samplePhoto" alt="Holzprodukte der Schülerfirma" shape="round" plate="sage" tilt="right" />
      </div>

      <h3 class="sub-title">Ruhige Bilder: BaseImage (Karten, Galerien, Listen)</h3>
      <div class="image-frames">
        <BaseImage :src="samplePhoto" alt="Holzprodukte der Schülerfirma" frame="offset" ratio="1 / 1" />
        <BaseImage :src="samplePhoto" alt="Holzprodukte der Schülerfirma" frame="offset" ratio="1 / 1" />
        <BaseImage :src="samplePhoto" alt="Holzprodukte der Schülerfirma" frame="organic" ratio="1 / 1">
          <template #caption>3D-Druck</template>
        </BaseImage>
        <BaseImage alt="Platzhalter für ein Produktfoto" frame="plain" ratio="1 / 1" />
      </div>
    </BaseSection>

    <BaseSection tone="cream">
      <BaseMediaText>
        <template #media>
          <div class="blob-stage">
            <BaseBlob color="white" offset class="blob-stage__blob" />
            <BaseImage :src="samplePhoto" alt="Holzprodukte der Schülerfirma" frame="blob" ratio="1 / 1" class="blob-stage__image" />
            <BaseSticker tilt="right" class="blob-stage__sticker">Frisch gedruckt</BaseSticker>
          </div>
        </template>

        <BaseHeading eyebrow="Media + Text" title="Nachhaltige Ideen" highlight="aus dem 3D-Drucker.">
          <p>
            Bild links, Text rechts. Mit <code>reverse</code> wird die Seite getauscht. Ein Blob
            hinter dem Bild und ein Sticker sind die erlaubte Dosis an Verspieltheit pro Block.
          </p>
        </BaseHeading>
        <BaseButton variant="primary" arrow to="/">Zu den Produkten</BaseButton>
      </BaseMediaText>
    </BaseSection>

    <BaseSection tone="light">
      <BaseMediaText reverse>
        <template #media>
          <BaseImage :src="samplePhoto" alt="Holzprodukte der Schülerfirma" />
        </template>

        <BaseHeading eyebrow="Media + Text, reverse" title="Handwerk" highlight="mit der BBS II." />
        <p>
          Gleicher Baustein, Bild rechts. Auf kleinen Bildschirmen stehen Bild und Text immer
          untereinander, das Bild zuerst.
        </p>
        <div class="tag-row">
          <BaseTag tone="sage">Holz</BaseTag>
          <BaseTag tone="blue">Kooperation</BaseTag>
          <BaseTag tone="terracotta">Neu</BaseTag>
          <BaseTag>Neutral</BaseTag>
        </div>
      </BaseMediaText>
    </BaseSection>

    <BaseSection tone="cream" spacing="md">
      <h3 class="sub-title">Karten</h3>
      <div class="card-grid">
        <BaseCard title="Schlüsselanhänger" tag="3D-Druck" interactive>
          <template #media>
            <BaseImage :src="samplePhoto" alt="" ratio="4 / 3" />
          </template>
          <p>Bunte Anhänger aus recyceltem Filament, jedes Stück ein Unikat.</p>
          <template #footer>
            <BaseButton variant="text" arrow>Mehr erfahren</BaseButton>
          </template>
        </BaseCard>
        <BaseCard title="Holzmöbel" tag="Handwerk" interactive>
          <template #media>
            <BaseImage :src="samplePhoto" alt="" ratio="4 / 3" />
          </template>
          <p>Handgefertigt in Kooperation mit der BBS II.</p>
          <template #footer>
            <BaseButton variant="text" arrow>Mehr erfahren</BaseButton>
          </template>
        </BaseCard>
        <BaseCard title="Karte ohne Bild" tag="Termin">
          <p>Für Termine, Hinweise oder kurze Infos. Ohne <code>interactive</code> gibt es keinen Hover-Effekt.</p>
          <template #footer>
            <BaseTag tone="terracotta">14.03.2027</BaseTag>
          </template>
        </BaseCard>
      </div>
    </BaseSection>

    <!-- Playful accents -->
    <BaseSection tone="light" spacing="md">
      <BaseHeading eyebrow="Komponenten" title="Spielerische Akzente" :level="2">
        <p>
          Sparsam einsetzen: höchstens ein Blob, ein Sticker und eine Deko pro Abschnitt. Alle sind rein
          dekorativ (<code>aria-hidden</code>) und werden vom Elternelement positioniert.
        </p>
      </BaseHeading>

      <h3 class="sub-title">BaseBlob</h3>
      <div class="accent-grid">
        <figure v-for="color in ['cream', 'sage', 'terracotta', 'blue'] as const" :key="color">
          <BaseBlob :color="color" :shape="color === 'sage' || color === 'blue' ? 2 : 1" />
          <figcaption><code>{{ color }}</code></figcaption>
        </figure>
        <figure>
          <BaseBlob color="cream" offset />
          <figcaption><code>offset</code></figcaption>
        </figure>
      </div>

      <h3 class="sub-title">BaseSticker</h3>
      <div class="demo-row demo-row--roomy">
        <BaseSticker>Mit Ideen gemacht!</BaseSticker>
        <BaseSticker tone="blue" tilt="right">Neu im Sortiment</BaseSticker>
        <BaseSticker tone="cream">Handmade</BaseSticker>
      </div>

      <h3 class="sub-title">BaseDecor</h3>
      <div class="demo-row demo-row--roomy">
        <BaseDecor kind="star" />
        <BaseDecor kind="star" color="blue" spin />
        <BaseDecor kind="dots" />
        <BaseDecor kind="dots" color="blue" />
        <BaseDecor kind="rays" />
        <BaseDecor kind="rays" color="sage" />
      </div>
    </BaseSection>

    <!-- Dark section -->
    <BaseSection tone="dark">
      <div class="dark-demo">
        <BaseHeading
          eyebrow="Dunkler Abschnitt"
          title="Kommt vorbei!"
          highlight="Wir freuen uns auf euch."
          inverse
        >
          <p>
            Mit <code>tone="dark"</code> und <code>inverse</code> an der Überschrift. Schatten werden hier
            automatisch Terrakotta.
          </p>
        </BaseHeading>
        <div class="demo-row">
          <BaseButton variant="accent" arrow to="/kontakt">Kontakt aufnehmen</BaseButton>
          <BaseSticker tone="cream" tilt="right">Kommt vorbei!</BaseSticker>
        </div>
        <BaseDecor kind="star" spin class="dark-demo__rays" />
      </div>
    </BaseSection>
  </main>
</template>

<style scoped>
code {
  padding: 0.1em 0.35em;
  border-radius: var(--radius-sm);
  background: var(--color-cream);
  font-size: 0.85em;
}

.sub-title {
  margin: var(--space-lg) 0 var(--space-sm);
  font-family: var(--font-body), sans-serif;
  font-size: var(--text-sm);
  font-weight: var(--weight-bold);
  letter-spacing: var(--tracking-label);
  text-transform: uppercase;
}

.note {
  margin-top: var(--space-sm);
  font-size: var(--text-sm);
}

/* Intro */
.intro {
  display: grid;
  grid-template-columns: 1.2fr 1fr;
  align-items: center;
  gap: var(--space-xl);
}

.intro__art {
  position: relative;
  width: 100%;
  max-width: 22rem;
  margin-left: auto;
}

.intro__blob {
  position: absolute;
  inset: 0;
}

.intro__image {
  position: relative;
  width: 78%;
  margin: 11%;
}

.intro__rays {
  position: absolute;
  top: 0;
  right: 0;
}

.intro__dots {
  position: absolute;
  right: -1.5rem;
  bottom: -1rem;
}

/* Sticks out well beyond the blob's left edge, so it reads as a separate layer */
.intro__sticker {
  position: absolute;
  bottom: 10%;
  left: -22%;
}

/* Colours */
.swatches {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(10rem, 1fr));
  gap: var(--space-md);
  margin: var(--space-lg) 0 0;
  padding: 0;
  list-style: none;
}

.swatch {
  display: flex;
  flex-direction: column;
  gap: var(--space-xs);
  font-size: var(--text-sm);
}

.swatch__color {
  height: 5rem;
  border: var(--border);
  border-radius: var(--radius-md);
}

/* Typography */
.type-scale {
  margin: var(--space-lg) 0 0;
  padding: 0;
  list-style: none;
}

.type-scale__row {
  display: grid;
  grid-template-columns: 8rem 1fr;
  align-items: baseline;
  gap: var(--space-sm);
  padding: var(--space-xs) 0;
  border-bottom: 1px solid var(--color-grey);
}

.type-scale__row .is-display {
  font-family: var(--font-display), serif;
  font-weight: var(--weight-heading);
  letter-spacing: var(--tracking-heading);
  line-height: var(--leading-heading);
}

/* Tokens */
.tokens-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-xl);
}

.spacing,
.radii {
  display: flex;
  flex-direction: column;
  gap: var(--space-sm);
  margin: 0;
  padding: 0;
  list-style: none;
}

.spacing li {
  display: grid;
  grid-template-columns: 7rem 5rem 1fr;
  align-items: center;
  gap: var(--space-sm);
  font-size: var(--text-sm);
}

.spacing__bar {
  height: 0.75rem;
  border-radius: var(--radius-pill);
  background: var(--color-blue);
}

.radii {
  flex-direction: row;
  flex-wrap: wrap;
  gap: var(--space-md);
}

.radii li {
  display: flex;
  flex-direction: column;
  gap: var(--space-xs);
  font-size: var(--text-sm);
}

.radii__box {
  width: 6rem;
  height: 4rem;
  border: var(--border);
  background: var(--color-cream);
}

.shadows {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-lg);
}

.shadows__box {
  display: grid;
  place-items: center;
  width: 11rem;
  height: 5rem;
  border: var(--border);
  border-radius: var(--radius-md);
  background: var(--color-white);
  box-shadow: var(--shadow-offset) var(--shadow-color);
  font-size: var(--text-sm);
  text-align: center;
}

.shadows__box--dark {
  --shadow-color: var(--color-terracotta);

  border-color: var(--color-ink);
  background: var(--color-ink);
  color: var(--color-white);
}

/* Demos */
.demo-rows {
  display: flex;
  flex-direction: column;
  gap: var(--space-md);
  margin-top: var(--space-lg);
}

.demo-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--space-md);
}

.demo-row--roomy {
  gap: var(--space-xl);
  padding: var(--space-xs) 0;
}

.demo-form {
  display: flex;
  flex-direction: column;
  gap: var(--space-md);
  padding: var(--space-lg);
  border-radius: var(--radius-organic);
  background: var(--color-cream);
}

.demo-form__row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--space-md);
}

.demo-form__submit {
  align-self: flex-start;
}

.image-frames {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr));
  gap: var(--space-xl);
}

.frame-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(13rem, 1fr));
  align-items: center;
  gap: var(--space-xl);
  padding: var(--space-md);
}

.blob-stage {
  position: relative;
  display: grid;
  place-items: center;
  max-width: 28rem;
  margin: 0 auto;
}

/* Blob fills the stage; the blob-shaped image sits centred on it */
.blob-stage__blob {
  position: absolute;
  inset: 0;
}

.blob-stage__image {
  position: relative;
  width: 78%;
  margin: 11%;
}

.blob-stage__sticker {
  position: absolute;
  top: var(--space-sm);
  right: 0;
}

.tag-row {
  display: flex;
  flex-wrap: wrap;
  gap: var(--space-xs);
}

.card-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(16rem, 1fr));
  gap: var(--space-lg);
}

.accent-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(8rem, 1fr));
  gap: var(--space-lg);
}

.accent-grid figure {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--space-xs);
  margin: 0;
  font-size: var(--text-sm);
}

.dark-demo {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-lg);
}

.dark-demo code {
  background: transparent;
  box-shadow: inset 0 0 0 1px var(--color-grey);
}

.dark-demo__rays {
  position: absolute;
  top: 0;
  right: 0;
}

@media (max-width: 800px) {
  .intro,
  .tokens-grid,
  .demo-form__row {
    grid-template-columns: 1fr;
  }

  .intro__art {
    margin: 0 auto;
  }
}
</style>
