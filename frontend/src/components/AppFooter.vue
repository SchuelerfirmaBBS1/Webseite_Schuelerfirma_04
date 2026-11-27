
<template>
  <footer class="site-footer">
    <div class="footer-inner">

      <!-- Top section -->
      <div class="footer-top">

        <!-- Brand -->
        <div class="footer-brand">
          <RouterLink to="/" class="footer-logo">
            <img
              src="/images/white_logo.png"
              alt="PRINT&BITE – ein Schülerunternehmen der BBS I Lüneburg"
            />
          </RouterLink>

          <div class="footer-brand-text">
            <p class="footer-subtitle">
              SCHÜLERUNTERNEHMEN · BBS I LÜNEBURG
            </p>

            <p class="footer-description">
              Kreative Ideen. Nachhaltige Produkte. Echter Genuss.
              Wir verbinden moderne Technik, kreatives Handwerk
              und gemeinsames Engagement.
            </p>
          </div>
        </div>

        <!-- Location -->
        <div class="footer-location">
          <div class="location-info">
            <h3>HIER FINDEST DU UNS</h3>

            <p class="footer-school-name">BBS I Lüneburg</p>
            <p>Spillbrunnenweg 1</p>
            <p>21337 Lüneburg</p>

            <a
              href="https://www.google.com/maps/dir/?api=1&destination=BBS+I+L%C3%BCneburg%2C+Spillbrunnenweg+1%2C+21337+L%C3%BCneburg"
              target="_blank"
              rel="noopener noreferrer"
              class="route-link"
            >
              Route planen <span aria-hidden="true">↗</span>
            </a>
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

      <!-- Bottom navigation section -->
      <div class="footer-links">

        <div class="footer-link-group">
          <h3>ENTDECKEN</h3>

          <nav class="footer-nav" aria-label="Footer-Navigation">
            <RouterLink to="/">Startseite</RouterLink>
            <a href="/#produkte">Produkte</a>
            <RouterLink to="/termine">Termine</RouterLink>
            <RouterLink to="/archiv">Archiv</RouterLink>
            <a href="/#kontakt">Kontakt</a>
          </nav>
        </div>

        <div class="footer-link-group">
          <h3>RECHTLICHES</h3>

          <nav class="footer-nav" aria-label="Rechtliche Informationen">
            <RouterLink to="/impressum">Impressum</RouterLink>
            <RouterLink to="/datenschutz">Datenschutz</RouterLink>
          </nav>
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
.site-footer {
  padding: 3.5rem 7% 1.4rem;
  background: #1e222c;
  color: #ffffff;
}

.footer-inner {
  max-width: 1400px;
  margin: 0 auto;
}

/* Top section */
.footer-top {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: center;
  gap: 5rem;
  padding-bottom: 3rem;
}

/* Brand */
.footer-brand {
  display: flex;
  align-items: center;
  gap: 2rem;
}

.footer-logo {
  display: block;
  flex-shrink: 0;
  width: 1px;
}

.footer-logo img {
  display: block;
  width: 100%;
  height: auto;
  object-fit: contain;
}

.footer-brand-text {
  max-width: 310px;
}

.footer-subtitle {
  margin: 0 0 0.9rem;
  color: #d9a477;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.1em;
  line-height: 1.6;
}

.footer-description {
  margin: 0;
  color: #c5cbd2;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.9rem;
  line-height: 1.9;
}

/* Location */
.footer-location {
  display: grid;
  grid-template-columns: 0.8fr 1.2fr;
  align-items: center;
  gap: 1.8rem;
}

.location-info h3,
.footer-link-group h3 {
  margin: 0 0 1.1rem;
  color: #d9a477;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.75rem;
  font-weight: 700;
  letter-spacing: 0.12em;
}

.location-info p {
  margin: 0;
  color: #c5cbd2;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.9rem;
  line-height: 1.9;
}

.location-info .footer-school-name {
  color: #ffffff;
  font-weight: 600;
}

.route-link {
  display: inline-flex;
  align-items: center;
  gap: 0.6rem;
  margin-top: 1.1rem;
  padding-bottom: 0.3rem;
  border-bottom: 1px solid #d9a477;
  color: #ffffff;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.85rem;
  font-weight: 600;
  text-decoration: none;
  transition: gap 0.2s ease, color 0.2s ease;
}

.route-link:hover {
  gap: 1rem;
  color: #d9a477;
}

/* Map */
.footer-map {
  position: relative;
  display: block;
  height: 165px;
  overflow: hidden;
  border: 1px solid rgb(255 255 255 / 12%);
  border-radius: 12px;
  background: #394b64;
}

.footer-map iframe {
  display: block;
  width: 100%;
  height: 100%;
  border: 0;
  filter: grayscale(25%);
  pointer-events: none;
}

.map-overlay {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  padding: 0.7rem;
  background: rgb(30 34 44 / 88%);
  color: #ffffff;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.75rem;
  text-align: center;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.footer-map:hover .map-overlay,
.footer-map:focus-visible .map-overlay {
  opacity: 1;
}

/* Navigation and legal links */
.footer-links {
  display: flex;
  align-items: flex-start;
  gap: 6rem;
  padding: 2rem 0;
  border-top: 1px solid rgb(255 255 255 / 15%);
}

.footer-link-group {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.footer-link-group h3 {
  margin: 0;
}

.footer-nav {
  display: flex;
  flex-direction: row;
  align-items: center;
  flex-wrap: wrap;
  gap: 1.6rem;
}

.footer-nav a {
  color: #d1d7df;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.9rem;
  font-weight: 500;
  text-decoration: none;
  transition: color 0.2s ease;
}

.footer-nav a:hover {
  color: #d9a477;
}

/* Copyright */
.footer-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  padding-top: 1.2rem;
  border-top: 1px solid rgb(255 255 255 / 15%);
  color: #aeb7c2;
  font-family: "Trebuchet MS", sans-serif;
  font-size: 0.78rem;
}

.footer-bottom p {
  margin: 0;
}

/* Responsive */
@media (max-width: 1100px) {
  .footer-top {
    gap: 3rem;
  }

  .footer-brand {
    gap: 1.2rem;
  }

  .footer-logo {
    width: 125px;
  }

  .footer-location {
    grid-template-columns: 1fr;
    gap: 1.2rem;
  }

  .footer-map {
    height: 150px;
  }

  .footer-links {
    gap: 3rem;
  }
}

@media (max-width: 700px) {
  .site-footer {
    padding: 2.8rem 1.5rem 1rem;
  }

  .footer-top {
    grid-template-columns: 1fr;
    gap: 2.5rem;
    padding-bottom: 2.5rem;
  }

  .footer-brand {
    gap: 1.5rem;
  }

  .footer-logo {
    width: 125px;
  }

  .footer-brand-text {
    max-width: 360px;
  }

  .footer-location {
    grid-template-columns: 1fr 1fr;
  }

  .footer-map {
    height: 130px;
  }

  .footer-links {
    flex-direction: column;
    gap: 2rem;
  }

  .footer-nav {
    gap: 1rem;
  }
}

@media (max-width: 480px) {
  .footer-brand {
    align-items: flex-start;
    gap: 1rem;
  }

  .footer-logo {
    width: 95px;
  }

  .footer-subtitle {
    font-size: 0.6rem;
  }

  .footer-description {
    font-size: 0.8rem;
  }

  .footer-location {
    grid-template-columns: 1fr;
  }

  .footer-map {
    height: 160px;
  }

  .footer-nav {
    gap: 0.8rem 1rem;
  }

  .footer-bottom {
    align-items: flex-start;
    flex-direction: column;
    gap: 0.6rem;
  }
}
</style>
