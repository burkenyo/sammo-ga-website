// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { lazy, requireTruthy, isTrue } from "@shared/utils";
import { type ApiRunner, DefaultApiRunner } from "./services/apiRunner";
import { ContainerBuilder, serviceKey, ServiceLifetime } from "@shared/dependencyInjection";
import * as serviceNames from "./serviceNames";
import { ExpansionsDb } from "./services/expansionsDb";
import { MockApiRunner } from "./services/mockApiRunner";

export const serviceKeys = {
  expansionsDb: serviceKey<ExpansionsDb>(serviceNames.expansionsDb),
  apiRunner: serviceKey<ApiRunner>(serviceNames.apiRunner)
} as const;

export const useServices = lazy(() => {
  const builder = new ContainerBuilder();

  if (isTrue(import.meta.env.VITE__USE_MOCK_API)) {
    builder.registerInjected(ServiceLifetime.Singleton, serviceKeys.apiRunner, MockApiRunner)
  } else {
    builder.registerInjected(ServiceLifetime.Singleton, serviceKeys.expansionsDb, ExpansionsDb);

    const apiBaseUrl =
      new URL(requireTruthy(import.meta.env.VITE__API_BASE_URL, "Required env value VITE_API_BASE_URL was not set!"));

    builder.registerFactory(ServiceLifetime.Singleton, serviceKeys.apiRunner,
      provider => new DefaultApiRunner(apiBaseUrl, provider(serviceKeys.expansionsDb)));
  }

  return builder.build();
});
