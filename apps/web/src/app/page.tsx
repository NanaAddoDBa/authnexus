export default function Home() {
  return (
    <main className="mx-auto flex min-h-screen max-w-3xl items-center px-6 py-16">
      <section aria-labelledby="foundation-heading" className="space-y-5">
        <p className="text-sm font-semibold tracking-[0.16em] text-sky-700 uppercase">
          AuthNexus
        </p>
        <h1 id="foundation-heading" className="text-4xl font-semibold tracking-tight text-slate-950 sm:text-5xl">
          Universal Authentication Platform
        </h1>
        <p className="max-w-2xl text-lg leading-8 text-slate-700">
          The V0.1 repository foundation is being established. Authentication experiences and
          provider integrations are introduced only through their planned, versioned capability
          slices.
        </p>
      </section>
    </main>
  );
}
