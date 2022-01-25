<template>
  <router-view/>
</template>

<style lang="scss">
@font-face {
  font-family: "SansaPro";
  src: local("SansaPro"),
    url(./fonts/SansaPro-Normal.otf) format("truetype");
}

#app {
  color: #2c3e50;
  font-family: "SansaPro";
}

#nav {
  padding: 30px;

  a {
    font-weight: bold;
    color: #2c3e50;

    &.router-link-exact-active {
      color: #42b983;
    }
  }
}

*::before,
*::after {
  box-sizing: border-box;
}

body {

  --dark-blue-color: #024d9d;
  --light-blue-color: #1a7ebe;

  display: grid;
  min-height: 100vh;
  line-height: 1.6;
  background: white;
  font-family: sans-serif;
  justify-content: center;
}

</style>
