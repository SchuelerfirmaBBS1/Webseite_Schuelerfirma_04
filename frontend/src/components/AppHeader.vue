
<script setup lang="ts">
</script>

<template>
  <header class="header">
    <div class="header-inner">

      <!-- Brand -->
      <RouterLink to="/" class="brand" aria-label="PRINT&BITE – Startseite">
        <span class="brand-name">
          PRINT<span class="brand-ampersand">&</span>BITE
        </span>

        <span class="brand-divider"></span>

        <span class="brand-subtitle">
          Schülerfirma
          <span>der BBS I Lüneburg</span>
        </span>
      </RouterLink>

      <!-- Navigation -->
      <nav class="navigation" aria-label="Hauptnavigation">
        <RouterLink to="/" class="nav-link">Startseite</RouterLink>
        <a href="/#produkte" class="nav-link">Produkte</a>
        <RouterLink to="/termine" class="nav-link">Termine</RouterLink>
        <RouterLink to="/archiv" class="nav-link">Archiv</RouterLink>
        <a href="/#ueber-uns" class="nav-link">Über uns</a>
        <a href="/#kontakt" class="contact-link">
          Kontakt <span aria-hidden="true">?</span>
        </a>
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
  background: #202d43;
  border-bottom: 1px solid rgb(255 255 255 / 12%);
}

.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
  max-width: 1500px;
  margin: 0 auto;
  padding: 0.85rem 7%;
}

/* Brand */
.brand {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  gap: 1rem;
  color: #ffffff;
  letter-spacing: 0.12em;
  text-decoration: none;
}

.brand-name {
  font-family: 'Cormorant Garamond', Georgia, serif;
  font-size: 2.35rem;
  font-weight: 600;
  letter-spacing: -0.055em;
  line-height: 1;
  white-space: nowrap;
}

.brand-ampersand {
  color: #a4aa91;
}

.brand-divider {
  width: 1px;
  height: 42px;
  background: rgb(255 255 255 / 30%);
}

.brand-subtitle {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  color: #ffffff;
  font-size: 0.85rem;
  font-weight: 600;
  letter-spacing: 0.02em;
  line-height: 1.4;
  white-space: nowrap;
}

.brand-subtitle span {
  color: #c5cbd2;
  font-size: 0.72rem;
  font-weight: 400;
}

/* Navigation */
.navigation {
  display: flex;
  align-items: center;
  gap: 2rem;
}

.nav-link {
  position: relative;
  padding: 0.5rem 0;
  color: #d1d7df;
  font-size: 0.86rem;
  font-weight: 500;
  text-decoration: none;
  transition: color 0.2s ease;
}

.nav-link::after {
  position: absolute;
  right: 0;
  bottom: 0;
  left: 0;
  height: 1px;
  background: #a4aa91;
  content: "";
  transform: scaleX(0);
  transform-origin: left;
  transition: transform 0.25s ease;
}

.nav-link:hover {
  color: #a4aa91;
}

.nav-link:hover::after {
  transform: scaleX(1);
}

/* Active page */
.nav-link.router-link-active {
  color: #ffffff;
  font-weight: 700;
}

.nav-link.router-link-active::after {
  transform: scaleX(1);
}

/* Contact button */
.contact-link {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  padding: 0.7rem 1.3rem;
  border: 1px solid #a4aa91;
  border-radius: 50px;
  background: transparent;
  color: #ffffff;
  font-size: 0.84rem;
  font-weight: 500;
  text-align: center;
  text-decoration: none;
  transition: all 0.25s ease;
}

.contact-link:hover {
  background: #a4aa91;
  color: #202d43;
}

.contact-link span {
  color: #a4aa91;
}

.contact-link:hover span {
  color: #202d43;
}

/* Responsive */
@media (max-width: 1150px) {
  .header-inner {
    padding-right: 4%;
    padding-left: 4%;
    gap: 1.5rem;
  }

  .brand-name {
    font-size: 2rem;
  }

  .brand {
    gap: 0.7rem;
  }

  .navigation {
    gap: 1.2rem;
  }

  .nav-link {
    font-size: 0.82rem;
  }
}

@media (max-width: 850px) {
  .header-inner {
    flex-direction: column;
    gap: 1rem;
    padding: 1rem;
  }

  .navigation {
    justify-content: center;
    flex-wrap: wrap;
    gap: 0.7rem 1.3rem;
  }

  .contact-link {
    padding: 0.55rem 0.9rem;
  }
}

@media (max-width: 420px) {
  .brand-name {
    font-size: 1.8rem;
  }

  .brand-subtitle {
    font-size: 0.76rem;
  }

  .brand-subtitle span {
    font-size: 0.65rem;
  }

  .brand-divider {
    height: 36px;
  }

  .navigation {
    gap: 0.6rem 1rem;
  }

  .nav-link,
  .contact-link {
    font-size: 0.78rem;
  }
}
</style>
