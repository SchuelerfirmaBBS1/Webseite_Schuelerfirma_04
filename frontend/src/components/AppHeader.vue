
<script setup lang="ts">
import { useI18n } from 'vue-i18n'

const { t, locale } = useI18n()

function toggleLanguage() {
  const newLanguage = locale.value === 'de' ? 'en' : 'de'

  locale.value = newLanguage
  localStorage.setItem('language', newLanguage)
}
</script>

<template>
  <header class="header">
    <div class="header-inner">

      <!-- Brand -->
      <RouterLink to="/" class="brand" aria-label="PRINT&BITE – Startseite">
        <img
          src="/images/logo-header.png"
          alt="PRINT&BITE"
          class="brand-logo"
        />

        <span class="brand-text">
          <span class="brand-name">PRINT&BITE</span>
          <span class="brand-subtitle">
            Schülerunternehmen
            <span>der BBS I Lüneburg</span>
          </span>
        </span>
      </RouterLink>

      <!-- Navigation -->
      <nav class="navigation" aria-label="Hauptnavigation">
        <RouterLink to="/" class="nav-link">
          {{ t('navigation.home') }}
        </RouterLink>

        <a href="/#produkte" class="nav-link">
          {{ t('navigation.products') }}
        </a>

        <RouterLink to="/termine" class="nav-link">
          {{ t('navigation.dates') }}
        </RouterLink>

        <RouterLink to="/archiv" class="nav-link">
          {{ t('navigation.archive') }}
        </RouterLink>

        <a href="/#ueber-uns" class="nav-link">
          {{ t('navigation.about') }}
        </a>

        <a href="/#kontakt" class="contact-link">
          {{ t('navigation.contact') }}
          <span aria-hidden="true">↗</span>
        </a>

        <!-- Language Switch -->
        <button
          class="language-button"
          type="button"
          @click="toggleLanguage"
          :aria-label="locale === 'de' ? 'Switch to English' : 'Zu Deutsch wechseln'"
        >
          {{ locale === 'de' ? 'EN' : 'DE' }}
        </button>
      </nav>

    </div>
  </header>
</template>

<style scoped>
.header {
  position: sticky;
  top: 0;
  z-index: 100;
  width: 100%;
  background: #1e222c;
  border-bottom: 1px solid rgb(255 255 255 / 10%);
}

.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
  max-width: 1500px;
  min-height: 74px;
  margin: 0 auto;
  padding: 0.5rem 5%;
}

/* Brand */
.brand {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: 0.9rem;
  color: #ffffff;
  text-decoration: none;
}

.brand-logo {
  display: block;
  width: 85px;
  height: auto;
  object-fit: contain;
}

.brand-text {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}

.brand-name {
  color: #ffffff;
  font-family: Georgia, serif;
  font-size: 1.45rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  line-height: 1.2;
}

.brand-subtitle {
  display: flex;
  flex-direction: column;
  color: #b8bec8;
  font-family: Arial, sans-serif;
  font-size: 0.68rem;
  font-weight: 400;
  letter-spacing: 0.025em;
  line-height: 1.4;
}

/* Navigation */
.navigation {
  display: flex;
  align-items: center;
  gap: 2rem;
}

.nav-link {
  position: relative;
  padding: 0.7rem 0;
  color: #e1e4e9;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 1rem;
  font-weight: 600;
  letter-spacing: 0.01em;
  text-decoration: none;
  transition: color 0.25s ease;
}

.nav-link::after {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  height: 2px;
  border-radius: 2px;
  background: #d9a477;
  content: "";
  transform: scaleX(0);
  transform-origin: left;
  transition: transform 0.25s ease;
}

.nav-link:hover,
.nav-link.router-link-active {
  color: #ffffff;
}

.nav-link:hover::after,
.nav-link.router-link-active::after {
  transform: scaleX(1);
}

/* Contact link */
.contact-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.7rem;
  padding: 0.85rem 1.5rem;
  border: 1px solid #d9a477;
  border-radius: 50px;
  background: #d9a477;
  color: #1e222c;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.95rem;
  font-weight: 700;
  text-decoration: none;
  transition: background 0.25s ease, color 0.25s ease;
}

.contact-link:hover {
  background: transparent;
  color: #d9a477;
}

.contact-link span {
  font-size: 1rem;
}

/* Language Switch */
.language-button {
  padding: 0.55rem 0.8rem;
  border: 1px solid rgb(255 255 255 / 35%);
  border-radius: 50px;
  background: transparent;
  color: #ffffff;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.85rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  cursor: pointer;
  transition: background 0.25s ease, color 0.25s ease, border-color 0.25s ease;
}

.language-button:hover {
  border-color: #d9a477;
  background: #d9a477;
  color: #1e222c;
}

/* Responsive */
@media (max-width: 1150px) {
  .header-inner {
    padding-right: 4%;
    padding-left: 4%;
    gap: 1.2rem;
  }

  .brand-logo {
    width: 105px;
  }

  .brand {
    gap: 0.7rem;
  }

  .brand-name {
    font-size: 1.25rem;
  }

  .navigation {
    gap: 1.1rem;
  }

  .nav-link {
    font-size: 0.9rem;
  }
}

@media (max-width: 850px) {
  .header-inner {
    flex-direction: column;
    justify-content: center;
    gap: 1rem;
    padding: 0.9rem 1rem;
  }

  .brand-logo {
    width: 115px;
  }

  .navigation {
    justify-content: center;
    flex-wrap: wrap;
    gap: 0.6rem 1.3rem;
  }

  .contact-link {
    padding: 0.6rem 1rem;
  }
}

@media (max-width: 420px) {
  .brand-logo {
    width: 95px;
  }

  .brand-name {
    font-size: 1.15rem;
  }

  .brand-subtitle {
    font-size: 0.62rem;
  }

  .navigation {
    gap: 0.5rem 0.9rem;
  }

  .nav-link,
  .contact-link {
    font-size: 0.76rem;
  }

  .language-button {
    font-size: 0.76rem;
    padding: 0.45rem 0.65rem;
  }
}
</style>
