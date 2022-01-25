<template>
  <transition name="cardfade" :duration="600">
    <div v-if="show" class="card" :style="`left: ${x + 20}px; top: ${y + 20}px;`">
        <div class="card-content">
              <h2 class="card-title"> {{title}} </h2>
              <p class="card-body"> # Abonnees: {{content}} </p>
        </div>
    </div>
  </transition>
</template>


<script>
// @ is an alias to /src
export default {
  name: 'Card',
  props: {
      x : Number,
      y : Number,
      show: Boolean,
      title: String,
      content: String
  }
}
</script>

<style >

.card {
  text-align: left;
  color: black;
  background-size: cover;
  background-color: hsl(0, 0%, 100%);
  width: max-content;
  border-radius: .5rem;
  overflow: hidden;

  box-shadow: 0 4px 8px 0 rgba(0,0,0,0.2);

  position: absolute;
}



/* .card:hover .card-body {
  transform: translateX(0);
  transition-delay: 300ms;
} */

.card-content {
  --padding: 1rem;
  padding: var(--padding);
}


.card-title {
  position: relative;
  width: max-content;
  color: var(--dark-blue-color);
}


.card-title::after {
  content: "";
  position: absolute;
  height: 3px;
  left: calc(var(--padding) * -1);
  bottom: 0px;
  background: var(--dark-blue-color);
  width: calc(100% + var(--padding));
  transform-origin: left;
  transform: scaleX(1);
}

.cardfade-enter-from {
  transform: scale(0);
}

.cardfade-enter-to {
  transform: scale(1);
}

.cardfade-enter-active {
  transition: all ease-out 200ms;
}

.cardfade-leave-to {
  transform: scale(0);
  opacity: 0;
}

.cardfade-leave-active {
  transition: all ease-in 300ms;
}

.cardfade-enter-from .card-title{
  transform: translateY(2rem);
  opacity: 0;
}

.cardfade-enter-to .card-title {
  transform: translateY(0rem);
  opacity: 1;
}

.cardfade-enter-active .card-title {
  transition: 200ms ease-out;
  transition-delay: 200ms;
}

.cardfade-enter-from .card-title::after {
  transform: scaleX(0);
}

.cardfade-enter-active .card-title::after {
  transition: ease-out 200ms;
  transition-delay: 200ms;
}

.cardfade-enter-from .card-body {
  transform: translateX(-100%);
  opacity: 0;
}

.cardfade-enter-to .card-body {
  transform: translateX(0);
  opacity: 1;
}

.cardfade-enter-active .card-body {
  transition: ease-out 200ms;
  transition-delay: 300ms;
}

</style>
