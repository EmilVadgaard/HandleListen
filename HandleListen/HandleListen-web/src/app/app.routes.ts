import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Handleliste } from './handleliste/handleliste';
import { authGuard } from './auth-guard';
import { ownerGuard } from './owner-guard';
import { Register } from './register/register';
import { ForgotPassword } from './forgot-password/forgot-password';
import { ResetPassword } from './reset-password/reset-password';
import { ConfirmEmail } from './confirm-email/confirm-email';
import { Kalender } from './kalender/kalender';
import { Madplaner } from './madplaner/madplaner';
import { PlanerListe } from './planer-liste/planer-liste';
import { PlanDetail } from './plan-detail/plan-detail';
import { Forum } from './forum/forum';
import { Admin } from './admin/admin';
import { PrivacyPolicy } from './privacy-policy/privacy-policy';
import { Settings } from './settings/settings';

export const routes: Routes = [
    { path: '', redirectTo: 'handleliste', pathMatch: 'full' },
    { path: 'login', component: Login },
    { path: 'handleliste', component: Handleliste, canActivate: [authGuard] },
    { path: 'kalender', component: Kalender, canActivate: [authGuard] },
    { path: 'madplaner', component: Madplaner, canActivate: [authGuard] },
    { path: 'planer', component: PlanerListe, canActivate: [authGuard] },
    { path: 'planer/:id', component: PlanDetail, canActivate: [authGuard] },
    { path: 'forum', component: Forum, canActivate: [authGuard] },
    { path: 'admin', component: Admin, canActivate: [authGuard, ownerGuard] },
    { path: 'indstillinger', component: Settings, canActivate: [authGuard] },
    { path: 'privatlivspolitik', component: PrivacyPolicy },
    { path: 'register', component: Register },
    { path: 'forgot-password', component: ForgotPassword },
    { path: 'reset-password', component: ResetPassword },
    { path: 'confirm-email', component: ConfirmEmail },
];
