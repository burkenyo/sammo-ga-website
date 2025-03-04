<!-- Copyright © 2025 Samuel Justin Speth Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<route>
{ meta: {
  title: "About",
  description: "Explore the story and process of creating the sammo.ga site."
} }
</route>

## About sammo.ga

This site represents a labor of love (or necessity?) as I endeavored to learn modern front-end web technologies and build a web site. It’s also a new place to showcase my [Mathematical Melodies project](/melodies). Back when I was studying composition full time, I wrote that page as a way to explore using mathematical constants intrinsic to the universe and their never-ending decimal expansions as input to generate musical melodies. It was a single giant HTML file with embedded scripts powered by JQuery.

I built this site from scratch using Vue and hand-written CSS. An ASP.Net Core minimal API powers the melodies page by retrieving interesting mathematical constants from the [Online Encyclopedia of Integer Sequences](https://oeis.org) (OEIS) and converting them to Base-12. I originally planned to do this entirely in the browser, but the lack of CORS headers from the OEIS search endpoint forced me to pivot to a back-end API. The side benefit is that the API caches the converted constants and avoids repeated, expensive calculations. The front-end also caches retrieved constants in IndexedDB.

The site is served by a static hosting service, and each page is pre-rendered using vite-ssg. There is a [staging site](https://icy-dune-034a1140f-dev.eastus2.2.azurestaticapps.net) for examining upcoming changes. Both the production and staging sites, as well as the back-end API, are deployed automatically via GitHub workflows, surfacing the latest changes as they are committed.
