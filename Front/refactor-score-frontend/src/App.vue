<script setup lang="ts">
import { RouterView } from 'vue-router';
</script>

<template>
  <div id="app">
    <nav class="navbar">
      <div class="navbar-brand">
        <router-link to="/" class="brand-link">
          <h1>RefactorScore</h1>
          <span class="subtitle">Clean Code Analysis</span>
        </router-link>
      </div>
      <div class="navbar-menu">
        <router-link to="/" class="nav-link">Dashboard</router-link>
        <router-link to="/analysis" class="nav-link">Análises</router-link>
        <router-link to="/statistics" class="nav-link">Estatísticas</router-link>
      </div>
    </nav>

    <main class="main-content">
      <RouterView />
    </main>

    <footer class="footer">
      <p>&copy; 2025 RefactorScore - TCC Project</p>
    </footer>
  </div>
</template>

<style lang="scss">
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen,
    Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;
  background: #f5f7fa;
  color: #2c3e50;
}

#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.navbar {
  background: white;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  padding: 1rem 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;

  .navbar-brand {
    .brand-link {
      text-decoration: none;
      display: flex;
      flex-direction: column;

      h1 {
        font-size: 1.5rem;
        color: #447bda;
        font-weight: bold;
      }

      .subtitle {
        font-size: 0.75rem;
        color: #7f8c8d;
        text-transform: uppercase;
        letter-spacing: 1px;
      }
    }
  }

  .navbar-menu {
    display: flex;
    gap: 2rem;

    .nav-link {
      text-decoration: none;
      color: #2c3e50;
      font-weight: 500;
      padding: 0.5rem 1rem;
      border-radius: 6px;
      transition: all 0.3s ease;

      &:hover {
        background: #f8f9fa;
        color: #447bda;
      }

      &.router-link-active {
        background: #447bda;
        color: white;
      }
    }
  }
}

.main-content {
  flex: 1;
  width: 100%;
}

.footer {
  background: white;
  padding: 1.5rem 2rem;
  text-align: center;
  border-top: 1px solid #e0e0e0;
  margin-top: 2rem;

  p {
    color: #7f8c8d;
    font-size: 0.9rem;
  }
}
</style>
