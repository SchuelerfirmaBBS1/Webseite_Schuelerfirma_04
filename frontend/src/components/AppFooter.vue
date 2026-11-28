<script setup lang="ts">
import logoText from '@/assets/brand/logo-text-light.svg'
import BaseButton from '@/components/base/BaseButton.vue'
</script>

<template>
  <footer class="site-footer">
    <div class="footer-inner">

      <!-- Top section -->
      <div class="footer-top">

        <!-- Brand -->
        <div class="footer-brand">
          <!-- Logo: text version, light for the dark footer (styleguide logo rules) -->
          <RouterLink to="/" class="footer-logo">
            <img :src="logoText" alt="PRINT&BITE – Startseite" />
          </RouterLink>

          <p class="footer-description">
            Kreative Ideen. Nachhaltige Produkte. Echter Genuss.
            Wir verbinden moderne Technik, kreatives Handwerk
            und gemeinsames Engagement.
          </p>
        </div>

        <!-- Link groups -->
        <div class="footer-link-group footer-link-group--discover">
          <h3 class="footer-label">Entdecken</h3>

          <nav class="footer-nav" aria-label="Footer-Navigation">
            <RouterLink to="/">Startseite</RouterLink>
            <a href="/#produkte">Produkte</a>
            <RouterLink to="/termine">Termine</RouterLink>
            <RouterLink to="/archiv">Archiv</RouterLink>
            <RouterLink to="/kontakt">Kontakt</RouterLink>
          </nav>
        </div>

        <div class="footer-link-group footer-link-group--legal">
          <h3 class="footer-label">Rechtliches</h3>

          <nav class="footer-nav" aria-label="Rechtliche Informationen">
            <RouterLink to="/impressum">Impressum</RouterLink>
            <RouterLink to="/datenschutz">Datenschutz</RouterLink>
          </nav>
        </div>

        <!-- Location -->
        <div class="footer-location">
          <div class="location-info">
            <h3 class="footer-label">Hier findest du uns</h3>

            <p class="footer-school-name">BBS I Lüneburg</p>
            <p>Spillbrunnenweg 1</p>
            <p>21337 Lüneburg</p>

            <BaseButton
              variant="text"
              size="sm"
              arrow
              href="https://www.google.com/maps/dir/?api=1&destination=BBS+I+L%C3%BCneburg%2C+Spillbrunnenweg+1%2C+21337+L%C3%BCneburg"
              target="_blank"
              rel="noopener noreferrer"
              class="route-link"
            >
              Route planen
            </BaseButton>
          </div>

          <a
            href="https://www.google.com/maps/search/?api=1&query=BBS+I+L%C3%BCneburg%2C+Spillbrunnenweg+1%2C+21337+L%C3%BCneburg"
            target="_blank"
            rel="noopener noreferrer"
            class="footer-map"
            aria-label="BBS I Lüneburg auf Google Maps öffnen"
          >
            <iframe
              src="https://maps.google.com/maps?q=BBS%20I%20L%C3%BCneburg%2C%20Spillbrunnenweg%201%2C%2021337%20L%C3%BCneburg&t=&z=15&ie=UTF8&iwloc=&output=embed"
              title="Standort der BBS I Lüneburg auf Google Maps"
              loading="lazy"
              referrerpolicy="no-referrer-when-downgrade"
              tabindex="-1"
            ></iframe>
            <span class="map-overlay">In Google Maps öffnen ↗</span>
          </a>
        </div>

      </div>

      <!-- Copyright -->
      <div class="footer-bottom">
        <p>© 2026 PRINT&BITE · HA26B</p>
        <p>Ein Schülerunternehmen der BBS I Lüneburg</p>
      </div>

    </div>
  </footer>
</template>

<style scoped>
/* Built on the design tokens (see /styleguide); dark surface, so shadows are terracotta */
.site-footer {
  --shadow-color: var(--color-terracotta);
  --color-fg: var(--color-white);
  --color-bg: var(--color-ink);
  --footer-line: 1px solid color-mix(in srgb, var(--color-white) 15%, transparent);

  padding: var(--space-xl) 0 var(--space-md);
  background: var(--color-ink);
  color: var(--color-white);
  font-size: var(--text-sm);
}

/* Same container as BaseSection and the header */
.footer-inner {
  max-width: var(--container-width);
  margin: 0 auto;
  padding: 0 var(--container-padding);
}

/* One row: brand | link groups | location (text + map) */
.footer-top {
  display: grid;
  grid-template-areas: 'brand discover legal location';
  grid-template-columns: minmax(14rem, 1.2fr) auto auto minmax(0, 2fr);
  align-items: start;
  gap: var(--space-lg);
  padding-bottom: var(--space-lg);
}

.footer-brand {
  grid-area: brand;
}

.footer-link-group--discover {
  grid-area: discover;
}

.footer-link-group--legal {
  grid-area: legal;
}

/* Logo above the text, so the wide wordmark keeps its clear space */
.footer-brand {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-md);
}

.footer-logo {
  display: block;
  width: 220px;
}

.footer-logo img {
  display: block;
  width: 100%;
  height: auto;
}

.footer-description {
  max-width: 20rem;
}

/* Small uppercase labels, terracotta on the dark surface */
.footer-label {
  margin-bottom: var(--space-sm);
  color: var(--color-terracotta);
  font-family: var(--font-body), sans-serif;
  font-size: var(--text-xs);
  font-weight: var(--weight-bold);
  letter-spacing: var(--tracking-label);
  text-transform: uppercase;
}

/* Location */
.footer-location {
  display: grid;
  grid-area: location;
  grid-template-columns: 0.8fr 1.2fr;
  align-items: start;
  gap: var(--space-lg);
}

.footer-school-name {
  font-weight: var(--weight-bold);
}

.route-link {
  margin-top: var(--space-sm);
}

/* Map */
.footer-map {
  position: relative;
  display: block;
  height: 165px;
  overflow: hidden;
  border: 2px solid var(--color-white);
  border-radius: var(--radius-md);
  background: var(--color-blue);
}

.footer-map iframe {
  display: block;
  width: 100%;
  height: 100%;
  border: 0;
  pointer-events: none;
}

.map-overlay {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  padding: var(--space-xs);
  background: var(--color-ink);
  color: var(--color-white);
  font-size: var(--text-xs);
  text-align: center;
  opacity: 0;
  transition: opacity var(--duration-fast) var(--ease-out);
}

.footer-map:hover .map-overlay,
.footer-map:focus-visible .map-overlay {
  opacity: 1;
}

/* Link groups: label with the links stacked below */
.footer-nav {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: var(--space-xs);
}

.footer-nav a {
  color: var(--color-white);
  text-decoration: none;
  text-underline-offset: 0.3em;
}

.footer-nav a:hover {
  color: var(--color-white);
  text-decoration: underline;
  text-decoration-color: var(--color-terracotta);
}

/* Copyright */
.footer-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--space-sm);
  padding-top: var(--space-md);
  border-top: var(--footer-line);
  font-size: var(--text-xs);
}

/* Responsive */
@media (max-width: 1100px) {
  /* Brand and links on top, location below */
  .footer-top {
    grid-template-areas:
      'brand discover legal'
      'location location location';
    grid-template-columns: 1fr auto auto;
  }

  .footer-logo {
    width: 200px;
  }

  .footer-map {
    height: 150px;
  }
}

@media (max-width: 700px) {
  .footer-top {
    grid-template-areas:
      'brand brand'
      'discover legal'
      'location location';
    grid-template-columns: 1fr 1fr;
  }
}

@media (max-width: 480px) {
  .footer-logo {
    width: 180px;
  }

  .footer-location {
    grid-template-columns: 1fr;
  }

  .footer-bottom {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
