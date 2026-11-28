<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import logoText from '@/assets/brand/logo-text-light.svg'
import BaseButton from '@/components/base/BaseButton.vue'
import ThemeToggle from '@/components/ThemeToggle.vue'

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
      <!-- Logo: text version, light for the dark header (styleguide logo rules) -->
      <RouterLink to="/" class="brand">
        <img :src="logoText" alt="PRINT&BITE – Startseite" class="brand-wordmark" />
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

        <BaseButton variant="secondary" size="sm" to="/kontakt" arrow>
          {{ t('navigation.contact') }}
        </BaseButton>

        <!-- Language Switch -->
        <button
          class="language-button"
          type="button"
          :aria-label="locale === 'de' ? 'Switch to English' : 'Zu Deutsch wechseln'"
          @click="toggleLanguage"
        >
          {{ locale === 'de' ? 'EN' : 'DE' }}
        </button>

        <!-- Light/dark mode (not functional yet) -->
        <ThemeToggle />
      </nav>

    </div>
  </header>
</template>

<style scoped>
/* Built on the design tokens (see /styleguide); dark surface, so shadows are terracotta */
.header {
  --shadow-color: var(--color-terracotta);
  --color-fg: var(--color-white);
  --color-bg: var(--color-ink);

  position: sticky;
  top: 0;
  z-index: 100;
  width: 100%;
  border-bottom: 1px solid color-mix(in srgb, var(--color-white) 12%, transparent);
  background: var(--color-ink);
  color: var(--color-white);
}

/* Same container as BaseSection, so logo and content line up */
.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-lg);
  max-width: var(--container-width);
  margin: 0 auto;
  padding: var(--space-sm) var(--container-padding);
}

.brand {
  flex-shrink: 0;
}

.brand-wordmark {
  display: block;
  width: 240px;
  height: auto;
}

/* Navigation */
.navigation {
  display: flex;
  align-items: center;
  gap: var(--space-md);
}

.nav-link {
  position: relative;
  padding: var(--space-xs) 0;
  color: var(--color-white);
  font-family: var(--font-body), sans-serif;
  font-size: var(--text-md);
  font-weight: var(--weight-bold);
  text-decoration: none;
}

/* Terracotta underline grows in on hover and marks the active page */
.nav-link::after {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  height: 2px;
  background: var(--color-terracotta);
  content: '';
  transform: scaleX(0);
  transform-origin: left;
  transition: transform var(--duration-normal) var(--ease-out);
}

/* RouterLink sets aria-current="page" on the link of the current page */
.nav-link:hover,
.nav-link[aria-current='page'] {
  color: var(--color-white);
}

.nav-link:hover::after,
.nav-link[aria-current='page']::after {
  transform: scaleX(1);
}

/* Language switch: small outlined pill */
.language-button {
  min-height: 36px;
  padding: 0 var(--space-xs);
  border: 2px solid var(--color-white);
  border-radius: var(--radius-pill);
  background: transparent;
  color: var(--color-white);
  font-family: var(--font-body), sans-serif;
  font-size: var(--text-sm);
  font-weight: var(--weight-bold);
  letter-spacing: 0.04em;
  cursor: pointer;
  transition:
    background-color var(--duration-normal) var(--ease-out),
    color var(--duration-normal) var(--ease-out);
}

.language-button:hover {
  background: var(--color-white);
  color: var(--color-ink);
}

/* Responsive */
@media (max-width: 1150px) {
  .header-inner {
    gap: var(--space-md);
  }

  .brand-wordmark {
    width: 200px;
  }

  .navigation {
    gap: var(--space-sm);
  }

  .nav-link {
    font-size: var(--text-sm);
  }
}

@media (max-width: 850px) {
  .header-inner {
    flex-direction: column;
    gap: var(--space-sm);
  }

  .brand-wordmark {
    width: 220px;
  }

  .navigation {
    flex-wrap: wrap;
    justify-content: center;
    gap: var(--space-xs) var(--space-md);
  }
}

@media (max-width: 420px) {
  .brand-wordmark {
    width: 180px;
  }

  .navigation {
    gap: var(--space-xs) var(--space-sm);
  }
}
</style>
