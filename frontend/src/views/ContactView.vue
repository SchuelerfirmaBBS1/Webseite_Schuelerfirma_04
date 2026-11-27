<script setup lang="ts">
import { reactive, ref } from 'vue'
import type { ContactForm } from '@/types/contact'

const form = reactive<ContactForm>({
  firstName: '',
  lastName: '',
  email: '',
  subject: '',
  message: '',
})

const feedback = ref('')

function submit() {
  // TODO: send form to the backend once the contact endpoint exists
  feedback.value = 'Das Formular ist noch nicht verbunden. Deine Nachricht wurde nicht versendet.'
}
</script>

<template>
  <main class="contact-page">
    <section class="contact-section" aria-labelledby="contact-title">
      <div class="contact-content">
        <div class="contact-copy">
          <p class="eyebrow">WIR SIND FÜR DICH DA</p>

          <h1 id="contact-title">
            Wir freuen uns
            <span>auf deine Nachricht.</span>
          </h1>

          <p class="contact-description">
            Du hast Fragen zu unseren Produkten, möchtest mehr über unser Schülerunternehmen
            erfahren oder mit uns in Kontakt treten? Dann schreib uns gerne!
          </p>

          <div class="contact-note">
            <span class="contact-note-symbol" aria-hidden="true">✉</span>
            <p>Wir freuen uns über dein Interesse an PRINT&amp;BITE und melden uns gerne bei dir.</p>
          </div>
        </div>

        <form class="contact-form" @submit.prevent="submit">
          <div class="form-row">
            <div class="form-group">
              <label for="contact-first-name">Vorname</label>
              <input
                id="contact-first-name"
                v-model="form.firstName"
                type="text"
                name="firstName"
                placeholder="Vorname"
                autocomplete="given-name"
                required
              />
            </div>

            <div class="form-group">
              <label for="contact-last-name">Nachname</label>
              <input
                id="contact-last-name"
                v-model="form.lastName"
                type="text"
                name="lastName"
                placeholder="Nachname"
                autocomplete="family-name"
                required
              />
            </div>
          </div>

          <div class="form-row">
            <div class="form-group">
              <label for="contact-email">Deine E-Mail-Adresse</label>
              <input
                id="contact-email"
                v-model="form.email"
                type="email"
                name="email"
                placeholder="name@beispiel.de"
                autocomplete="email"
                required
              />
            </div>

            <div class="form-group">
              <label for="contact-subject">Betreff</label>
              <input
                id="contact-subject"
                v-model="form.subject"
                type="text"
                name="subject"
                placeholder="Worum geht es?"
                required
              />
            </div>
          </div>

          <div class="form-group">
            <label for="contact-message">Deine Nachricht</label>
            <textarea
              id="contact-message"
              v-model="form.message"
              name="message"
              rows="5"
              placeholder="Schreib uns deine Nachricht …"
              required
            ></textarea>
          </div>

          <button type="submit" class="contact-submit">
            Nachricht senden
            <span aria-hidden="true">→</span>
          </button>

          <p v-if="feedback" class="contact-feedback" role="status">{{ feedback }}</p>

          <p class="contact-privacy">
            Hinweis: Das Formular ist noch nicht mit einem E-Mail-Versand verbunden. Deine Eingaben
            werden aktuell nicht übertragen.
          </p>
        </form>
      </div>
    </section>
  </main>
</template>

<style scoped src="../assets/css/home/contact.css"></style>

<style scoped>
/* Colour tokens used by contact.css (same values as on the front page) */
.contact-page {
  --navy: #202d43;
  --cream: #f5f1e8;
  --muted: #747a78;

  color: var(--navy);
  background: #ffffff;
}

.contact-section {
  min-height: 70vh;
  padding-top: 6rem;
  padding-bottom: 6rem;
}

.eyebrow {
  margin: 0 0 1.2rem;
  color: #858b70;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.16em;
}

/* contact.css styles the heading as h2 (front page section); here it is the page h1 */
.contact-copy h1 {
  margin: 0;
  font-size: clamp(2.8rem, 4.5vw, 4.5rem);
  font-weight: 500;
  letter-spacing: -0.06em;
  line-height: 1.1;
}

.contact-copy h1 span {
  display: block;
  color: #81896f;
}

.contact-submit:focus-visible {
  outline: 2px solid #d9a477;
  outline-offset: 3px;
}

@media (max-width: 1000px) {
  .contact-content {
    gap: 2.5rem;
  }
}

@media (max-width: 850px) {
  .contact-content {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 700px) {
  .contact-section {
    padding: 3.5rem 1.25rem;
  }
}

@media (max-width: 450px) {
  .form-row {
    grid-template-columns: 1fr;
  }

  .contact-form {
    padding: 1.25rem;
  }
}

@media (prefers-reduced-motion: reduce) {
  .contact-submit {
    transition: none;
  }
}
</style>
