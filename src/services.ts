import { lazy } from "@/utils";
import { type ApiRunner, DefaultApiRunner } from "@/services/apiRunner";
import { ContainerBuilder, serviceKey, ServiceLifetime } from "@/dependencyInjection";
import * as serviceNames from "@/serviceNames";
import { ExpansionsDb } from "@/services/expansionsDb";
import { InterestingConstants } from "@/services/interestingConstants";

export const serviceKeys = {
  expansionsDb: serviceKey<ExpansionsDb>(serviceNames.expansionsDb),
  apiRunner: serviceKey<ApiRunner>(serviceNames.apiRunner),
  interestingConstants: serviceKey<InterestingConstants>(serviceNames.interestingConstants)
} as const;

export const useServices = lazy(() => {
  const builder = new ContainerBuilder();

  builder.registerInjected(ServiceLifetime.Singleton, serviceKeys.expansionsDb, ExpansionsDb);

  const apiBaseUrl = new URL("https://sammo-ga-api.rofl.ninja/");
  builder.registerFactory(ServiceLifetime.Singleton, serviceKeys.apiRunner,
    provider => new DefaultApiRunner(apiBaseUrl, provider(serviceKeys.expansionsDb)));

  builder.registerInjected(ServiceLifetime.Singleton, serviceKeys.interestingConstants, InterestingConstants);

  return builder.build();
});
