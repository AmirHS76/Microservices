import { ApplicationConfig } from '@angular/core';
import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideIcons } from '@ng-icons/core';
import {
  lucideCheck,
  lucideCheckCheck,
  lucideDownload,
  lucideLogIn,
  lucideLogOut,
  lucideMessageCircle,
  lucideRefreshCw,
  lucideSearch,
  lucideSend,
  lucideShieldCheck,
  lucideShieldPlus,
  lucideUserPlus,
  lucideUsers,
  lucideWifi,
  lucideWifiOff
} from '@ng-icons/lucide';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withXhr(), withInterceptors([authInterceptor])),
    provideIcons({
      lucideCheck,
      lucideCheckCheck,
      lucideDownload,
      lucideLogIn,
      lucideLogOut,
      lucideMessageCircle,
      lucideRefreshCw,
      lucideSearch,
      lucideSend,
      lucideShieldCheck,
      lucideShieldPlus,
      lucideUserPlus,
      lucideUsers,
      lucideWifi,
      lucideWifiOff
    })
  ]
};
