import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Handleliste } from './handleliste/handleliste';
import { authGuard } from './auth-guard';
import { Register } from './register/register';

export const routes: Routes = [
    { path: '', redirectTo: 'handleliste', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'handleliste', component: Handleliste, canActivate: [authGuard] },
    { path: 'register', component: Register },
];
