
import { createI18n } from 'vue-i18n'

const messages = {
  de: {
    navigation: {
      home: 'Startseite',
      products: 'Produkte',
      dates: 'Termine',
      archive: 'Archiv',
      about: 'Über uns',
      contact: 'Kontakt',
    },
  },

  en: {
    navigation: {
      home: 'Home',
      products: 'Products',
      dates: 'Events',
      archive: 'Archive',
      about: 'About us',
      contact: 'Contact',
    },
  },
}

const savedLocale = localStorage.getItem('language')

const i18n = createI18n({
  legacy: false,
  locale: savedLocale === 'en' ? 'en' : 'de',
  fallbackLocale: 'de',
  messages,
})

export default i18n
