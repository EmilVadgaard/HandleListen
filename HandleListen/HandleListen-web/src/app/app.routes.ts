import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Handleliste } from './handleliste/handleliste';
import { authGuard } from './auth-guard';
import { Register } from './register/register';
import { ForgotPassword } from './forgot-password/forgot-password';
import { ResetPassword } from './reset-password/reset-password';
import { ConfirmEmail } from './confirm-email/confirm-email';
import { Kalender } from './kalender/kalender';
import { Madplaner } from './madplaner/madplaner';

export const routes: Routes = [
    { path: '', redirectTo: 'handleliste', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'handleliste', component: Handleliste, canActivate: [authGuard] },
    { path: 'kalender', component: Kalender, canActivate: [authGuard] },
    { path: 'madplaner', component: Madplaner, canActivate: [authGuard] },
    { path: 'register', component: Register },
    { path: 'forgot-password', component: ForgotPassword },
    { path: 'reset-password', component: ResetPassword },
    { path: 'confirm-email', component: ConfirmEmail },
];
