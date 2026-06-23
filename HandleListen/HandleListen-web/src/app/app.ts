import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {Handleliste} from './handleliste/handleliste';
import {Login} from './login/login';
import {Header} from './header/header';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('HandleListen-web');
}
