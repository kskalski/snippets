import { createApp } from 'vue'
import { store } from './store/index';
import "@popperjs/core";
import "bootstrap";
import './App.scss'
import App from './App.vue'
import router from './router/index'
import { FontAwesomeIcon } from "@fortawesome/vue-fontawesome";
import './fortawesome';

createApp(App)
    .use(router)
    .use(store)
    .component("font-awesome-icon", FontAwesomeIcon)
    .mount('#app')
