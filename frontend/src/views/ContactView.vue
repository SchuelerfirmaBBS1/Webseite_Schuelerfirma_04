<script setup lang="ts">
import { reactive } from 'vue'
import type { ContactForm } from '@/types/contact'

const form = reactive<ContactForm>({
  firstName: '',
  lastName: '',
  email: '',
  subject: '',
  message: '',
})

function submit() {
  // TODO: send form to the backend once the contact endpoint exists
}
</script>

<template>
  <section class="space-y-4">
    <h1 class="text-3xl font-bold">Kontakt</h1>
    <p>Hier steht später, wie man die Schülerfirma erreichen kann.</p>
  </section>

  <section class="mt-10 space-y-2">
    <h2 class="text-xl font-semibold">Kontaktformular</h2>
    <!-- Tailwind's base styles remove input borders, so i added in class "border" for the fields to be visible -->
    <form class="space-y-4" @submit.prevent="submit">
      <div>
        <label for="firstName" class="block">Vorname</label>
        <input id="firstName" v-model="form.firstName" type="text" class="border" required />
      </div>

      <div>
        <label for="lastName" class="block">Nachname</label>
        <input id="lastName" v-model="form.lastName" type="text" class="border" required />
      </div>

      <div>
        <label for="email" class="block">E-Mail</label>
        <input id="email" v-model="form.email" type="email" class="border" required />
      </div>

      <div>
        <label for="subject" class="block">Betreff</label>
        <input id="subject" v-model="form.subject" type="text" class="border" required />
      </div>

      <div>
        <label for="message" class="block">Nachricht</label>
        <textarea id="message" v-model="form.message" rows="5" class="border" required></textarea>
      </div>

      <button type="submit" class="border">Absenden</button>
    </form>
  </section>

  <section class="mt-10 space-y-2">
    <h2 class="text-xl font-semibold">Adresse</h2>
    <p>Platzhalter für Adresse und Anfahrt.</p>
  </section>
</template>
