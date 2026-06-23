import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {Handleliste} from './handleliste/handleliste';
import {Login} from './login/login';

@Component({
  selector: 'app-root',
  imports: [Handleliste, Login],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('HandleListen-web');
}
